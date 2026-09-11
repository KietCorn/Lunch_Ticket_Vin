"""
Kitchen display — SSE stream + snapshot endpoint.

The SSE stream pushes the current pending/ready order list every 2 seconds.
When the client disconnects, the generator exits cleanly.
The browser's EventSource fires onerror on connection loss, which surfaces
the disconnected banner shown in the wireframe.
"""
import asyncio
import json
from datetime import date as date_type
from fastapi import APIRouter, Depends, Request
from fastapi.responses import StreamingResponse
from sqlalchemy.orm import Session, joinedload
from app.database import get_db, SessionLocal
from app.models import Order, DailyMenu, OrderStatus
from app.schemas import KitchenOrderOut

router = APIRouter(prefix="/api/kitchen", tags=["kitchen"])

PUSH_INTERVAL_SECONDS = 2


def _get_active_orders(db: Session) -> list[KitchenOrderOut]:
    today = date_type.today().isoformat()
    orders = (
        db.query(Order)
        .options(
            joinedload(Order.student),
            joinedload(Order.timeslot),
            joinedload(Order.daily_menu_entry).joinedload(DailyMenu.menu_item),
        )
        .join(DailyMenu)
        .filter(
            DailyMenu.date == today,
            Order.status.in_([OrderStatus.pending, OrderStatus.ready]),
        )
        .order_by(Order.created_at)
        .all()
    )
    return [
        KitchenOrderOut(
            id=o.id,
            order_type=o.order_type,
            status=o.status,
            student_name=o.student.full_name,
            item_name=o.daily_menu_entry.menu_item.name,
            timeslot_label=o.timeslot.label,
            created_at=o.created_at.strftime("%H:%M"),
        )
        for o in orders
    ]


@router.get("/orders", response_model=list[KitchenOrderOut])
def snapshot(db: Session = Depends(get_db)):
    """Snapshot endpoint — used for initial page load before SSE connects."""
    return _get_active_orders(db)


@router.get("/stream")
async def stream(request: Request):
    """
    SSE endpoint. Each event is a JSON array of the current active orders.
    No auth required — this URL is the access control for the kitchen display.
    """
    async def event_generator():
        while True:
            if await request.is_disconnected():
                break
            db = SessionLocal()
            try:
                orders = _get_active_orders(db)
                payload = json.dumps([o.model_dump() for o in orders], default=str)
                yield f"data: {payload}\n\n"
            finally:
                db.close()
            await asyncio.sleep(PUSH_INTERVAL_SECONDS)

    return StreamingResponse(
        event_generator(),
        media_type="text/event-stream",
        headers={
            "Cache-Control": "no-cache",
            "X-Accel-Buffering": "no",   # disable nginx buffering if behind a proxy
        },
    )
