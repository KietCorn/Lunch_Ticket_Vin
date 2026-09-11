import secrets
from datetime import datetime, date as date_type, time as time_type
from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.orm import Session, joinedload
from app.database import get_db
from app.models import (
    Order, DailyMenu, Account, Transaction,
    OrderType, OrderStatus, TransactionType, OrderSource,
)
from app.schemas import OrderOut, PreOrderRequest, WalkInRequest, ConfirmDeliveryRequest, OkResponse
from app.auth import get_current_staff

router = APIRouter(prefix="/api/orders", tags=["orders"])

# QR code expires this many minutes after timeslot start (BR-06)
QR_EXPIRY_MINUTES = 15


@router.post("/preorder", response_model=OrderOut, status_code=201)
def place_preorder(body: PreOrderRequest, db: Session = Depends(get_db), _=Depends(get_current_staff)):
    daily_menu, account = _validate_order(db, body.student_id, body.daily_menu_id)

    # One pre-order per student per slot per day (BR-03)
    existing = db.query(Order).filter(
        Order.student_id == body.student_id,
        Order.daily_menu_id == body.daily_menu_id,
        Order.status != OrderStatus.cancelled,
    ).first()
    if existing:
        raise HTTPException(status_code=409, detail={"code": "DUPLICATE_ORDER", "message": "Student already has an order for this slot."})

    price = int(daily_menu.menu_item.price)
    order, txn = _create_order_and_deduct(
        db, body.student_id, daily_menu, account, price, OrderType.pre_order
    )

    # Generate QR token: card-prefix + random hex (matches wireframe format)
    active_card = next((c for c in order.student.cards if c.status.value == "active"), None)
    prefix = active_card.card_token[-5:] if active_card else "XXXXX"
    order.qr_token = f"{prefix}·{secrets.token_hex(2).upper()}"
    order.qr_expires_at = _qr_expiry(daily_menu.timeslot.start_time)
    db.commit()
    db.refresh(order)

    return _order_out(order)


@router.post("/walkin", response_model=OrderOut, status_code=201)
def place_walkin(body: WalkInRequest, db: Session = Depends(get_db), _=Depends(get_current_staff)):
    daily_menu, account = _validate_order(db, body.student_id, body.daily_menu_id)
    price = int(daily_menu.menu_item.price)
    order, txn = _create_order_and_deduct(
        db, body.student_id, daily_menu, account, price, OrderType.walk_in,
        placed_by_staff=body.staff_id,
    )
    db.commit()
    db.refresh(order)
    return _order_out(order)


@router.get("", response_model=list[OrderOut])
def list_orders(
    student_id: int = Query(default=None),
    date: str = Query(default=None),
    status: str = Query(default=None),
    db: Session = Depends(get_db),
    _=Depends(get_current_staff),
):
    q = db.query(Order).options(
        joinedload(Order.student),
        joinedload(Order.timeslot),
        joinedload(Order.daily_menu_entry).joinedload(DailyMenu.menu_item),
    )
    if student_id:
        q = q.filter(Order.student_id == student_id)
    if date:
        q = q.join(DailyMenu).filter(DailyMenu.date == date)
    else:
        q = q.join(DailyMenu).filter(DailyMenu.date == date_type.today().isoformat())
    if status:
        q = q.filter(Order.status == status)
    return [_order_out(o) for o in q.order_by(Order.created_at.desc()).all()]


@router.post("/{order_id}/confirm", response_model=OrderOut)
def confirm_delivery(
    order_id: int,
    body: ConfirmDeliveryRequest,
    db: Session = Depends(get_db),
    _=Depends(get_current_staff),
):
    order = _get_order_or_404(db, order_id)

    if order.status == OrderStatus.delivered:
        raise HTTPException(status_code=409, detail={"code": "ALREADY_DELIVERED", "message": "Order already delivered."})
    if order.status == OrderStatus.cancelled:
        raise HTTPException(status_code=409, detail={"code": "ORDER_CANCELLED", "message": "Cannot confirm a cancelled order."})

    # Validate QR expiry for pre-orders
    if order.order_type == OrderType.pre_order and body.source == OrderSource.qr_scan:
        if order.qr_expires_at and datetime.utcnow() > order.qr_expires_at:
            raise HTTPException(status_code=409, detail={"code": "QR_EXPIRED", "message": "QR code has expired."})

    order.status = OrderStatus.delivered
    order.source = body.source
    order.delivered_by = body.staff_id
    db.commit()
    db.refresh(order)
    return _order_out(order)


@router.post("/{order_id}/ready", response_model=OrderOut)
def mark_ready(order_id: int, db: Session = Depends(get_db), _=Depends(get_current_staff)):
    """Kitchen marks an order as ready for pickup."""
    order = _get_order_or_404(db, order_id)
    if order.status != OrderStatus.pending:
        raise HTTPException(status_code=409, detail={"code": "INVALID_STATUS", "message": f"Cannot mark ready from status '{order.status}'."})
    order.status = OrderStatus.ready
    db.commit()
    db.refresh(order)
    return _order_out(order)


@router.delete("/{order_id}", response_model=OkResponse)
def cancel_order(order_id: int, db: Session = Depends(get_db), _=Depends(get_current_staff)):
    """Cancel an order and refund the balance. Allowed only before the deadline (BR-04)."""
    order = _get_order_or_404(db, order_id)

    if order.status in (OrderStatus.delivered, OrderStatus.cancelled):
        raise HTTPException(status_code=409, detail={"code": "CANNOT_CANCEL", "message": f"Order in status '{order.status}' cannot be cancelled."})

    # Deadline: 30 minutes before timeslot start (BR-04)
    deadline = _cancel_deadline(order.timeslot.start_time)
    if datetime.utcnow() > deadline:
        raise HTTPException(status_code=409, detail={"code": "PAST_DEADLINE", "message": "Cancellation deadline has passed."})

    # Refund atomically
    account = db.query(Account).filter(Account.student_id == order.student_id).first()
    amount = int(order.amount_charged)
    account.balance = int(account.balance) + amount
    txn = Transaction(
        account_id=account.id,
        transaction_type=TransactionType.refund,
        amount=amount,
        balance_after=int(account.balance),
        reference_order=order.id,
        note="Order cancelled",
    )
    db.add(txn)

    # Restore slot
    order.daily_menu_entry.available_quantity += 1
    order.status = OrderStatus.cancelled
    db.commit()
    return OkResponse()


# ── Helpers ───────────────────────────────────────────────────────────────────

def _validate_order(db: Session, student_id: int, daily_menu_id: int):
    """Check student account and slot availability. Returns (daily_menu, account)."""
    from sqlalchemy.orm import joinedload as jl
    daily_menu = (
        db.query(DailyMenu)
        .options(jl(DailyMenu.timeslot), jl(DailyMenu.menu_item))
        .filter(DailyMenu.id == daily_menu_id)
        .first()
    )
    if not daily_menu:
        raise HTTPException(status_code=404, detail={"code": "MENU_NOT_FOUND", "message": "Menu entry not found."})
    if daily_menu.available_quantity <= 0:
        raise HTTPException(status_code=409, detail={"code": "NO_SLOTS", "message": "No slots remaining for this item."})

    account = db.query(Account).filter(Account.student_id == student_id).first()
    if not account:
        raise HTTPException(status_code=404, detail={"code": "ACCOUNT_NOT_FOUND", "message": "Account not found."})

    price = int(daily_menu.menu_item.price)
    if int(account.balance) < price:
        raise HTTPException(
            status_code=409,
            detail={"code": "INSUFFICIENT_BALANCE", "message": f"Balance ({int(account.balance)}) is less than item price ({price})."},
        )
    return daily_menu, account


def _create_order_and_deduct(
    db: Session,
    student_id: int,
    daily_menu: DailyMenu,
    account: Account,
    price: int,
    order_type: OrderType,
    placed_by_staff: int = None,
) -> tuple[Order, Transaction]:
    """Atomically create order, decrement slot, deduct balance, write transaction."""
    # Decrement slot
    daily_menu.available_quantity -= 1

    # Create order
    order = Order(
        student_id=student_id,
        daily_menu_id=daily_menu.id,
        timeslot_id=daily_menu.timeslot_id,
        order_type=order_type,
        status=OrderStatus.pending,
        amount_charged=price,
        placed_by_staff=placed_by_staff,
    )
    db.add(order)
    db.flush()  # get order.id before writing transaction

    # Deduct balance
    account.balance = int(account.balance) - price
    txn = Transaction(
        account_id=account.id,
        transaction_type=TransactionType.deduction,
        amount=-price,
        balance_after=int(account.balance),
        reference_order=order.id,
    )
    db.add(txn)
    db.flush()

    # Re-eager-load for response
    db.refresh(order)
    from sqlalchemy.orm import joinedload
    order = (
        db.query(Order)
        .options(
            joinedload(Order.student).joinedload(type(order.student).cards),
            joinedload(Order.timeslot),
            joinedload(Order.daily_menu_entry).joinedload(DailyMenu.menu_item),
        )
        .filter(Order.id == order.id)
        .first()
    )
    return order, txn


def _get_order_or_404(db: Session, order_id: int) -> Order:
    from sqlalchemy.orm import joinedload
    order = (
        db.query(Order)
        .options(
            joinedload(Order.student).joinedload(type(Order).cards if False else Order.student.property.mapper.class_.cards),
            joinedload(Order.timeslot),
            joinedload(Order.daily_menu_entry).joinedload(DailyMenu.menu_item),
        )
        .filter(Order.id == order_id)
        .first()
    )
    if not order:
        raise HTTPException(status_code=404, detail={"code": "ORDER_NOT_FOUND", "message": "Order not found."})
    return order


def _order_out(order: Order) -> OrderOut:
    return OrderOut(
        id=order.id,
        order_type=order.order_type,
        status=order.status,
        amount_charged=int(order.amount_charged),
        qr_token=order.qr_token,
        qr_expires_at=order.qr_expires_at,
        created_at=order.created_at,
        student=order.student,
        timeslot=order.timeslot,
        item_name=order.daily_menu_entry.menu_item.name,
    )


def _qr_expiry(start_time_str: str) -> datetime:
    """QR expires QR_EXPIRY_MINUTES after timeslot start on today's date."""
    from datetime import timedelta
    h, m = map(int, start_time_str.split(":"))
    today = date_type.today()
    start = datetime.combine(today, time_type(h, m))
    return start + timedelta(minutes=QR_EXPIRY_MINUTES)


def _cancel_deadline(start_time_str: str) -> datetime:
    """Cancel deadline is 30 minutes before timeslot start (BR-04)."""
    from datetime import timedelta
    h, m = map(int, start_time_str.split(":"))
    today = date_type.today()
    start = datetime.combine(today, time_type(h, m))
    return start - timedelta(minutes=30)
