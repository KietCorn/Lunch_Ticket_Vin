from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session
from app.database import get_db
from app.models import Account, Transaction, TransactionType, Student
from app.schemas import AccountOut, TransactionOut, TopUpRequest, TopUpResponse
from app.auth import get_current_staff

router = APIRouter(prefix="/api/students", tags=["accounts"])


@router.get("/{student_id}/account", response_model=AccountOut)
def get_account(student_id: int, db: Session = Depends(get_db), _=Depends(get_current_staff)):
    account = _get_account_or_404(db, student_id)
    return account


@router.post("/{student_id}/account/topup", response_model=TopUpResponse)
def top_up(student_id: int, body: TopUpRequest, db: Session = Depends(get_db), _=Depends(get_current_staff)):
    if body.amount <= 0:
        raise HTTPException(status_code=422, detail={"code": "INVALID_AMOUNT", "message": "Top-up amount must be positive."})

    account = _get_account_or_404(db, student_id)

    # Atomic: update balance + write transaction in one commit
    account.balance = int(account.balance) + body.amount
    txn = Transaction(
        account_id=account.id,
        transaction_type=TransactionType.top_up,
        amount=body.amount,
        balance_after=account.balance,
        actor_id=body.staff_id,
        note=body.note,
    )
    db.add(txn)
    db.commit()
    db.refresh(txn)

    return TopUpResponse(transaction=TransactionOut.model_validate(txn), new_balance=int(account.balance))


@router.get("/{student_id}/account/transactions", response_model=list[TransactionOut])
def list_transactions(
    student_id: int,
    limit: int = 50,
    db: Session = Depends(get_db),
    _=Depends(get_current_staff),
):
    account = _get_account_or_404(db, student_id)
    txns = (
        db.query(Transaction)
        .filter(Transaction.account_id == account.id)
        .order_by(Transaction.created_at.desc())
        .limit(limit)
        .all()
    )
    return txns


def _get_account_or_404(db: Session, student_id: int) -> Account:
    account = db.query(Account).filter(Account.student_id == student_id).first()
    if not account:
        raise HTTPException(status_code=404, detail={"code": "ACCOUNT_NOT_FOUND", "message": "Account not found for this student."})
    return account
