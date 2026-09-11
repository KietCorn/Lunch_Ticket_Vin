from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session
from app.database import get_db
from app.models import Student, Card, CardStatus
from app.schemas import StudentOut, CardOut, OkResponse
from app.auth import get_current_staff

router = APIRouter(prefix="/api/students", tags=["students"])


@router.get("/lookup", response_model=StudentOut)
def lookup_student(q: str, db: Session = Depends(get_db), _=Depends(get_current_staff)):
    """Look up a student by MSSV. Used by POS for both QR fallback and walk-in."""
    student = db.query(Student).filter(Student.student_id == q).first()
    if not student:
        raise HTTPException(status_code=404, detail={"code": "STUDENT_NOT_FOUND", "message": f"No student with ID '{q}'."})
    return student


@router.get("/{student_id}/cards", response_model=list[CardOut])
def list_cards(student_id: int, db: Session = Depends(get_db), _=Depends(get_current_staff)):
    student = _get_or_404(db, student_id)
    return student.cards


@router.post("/{student_id}/cards/{card_id}/lock", response_model=OkResponse)
def lock_card(student_id: int, card_id: int, db: Session = Depends(get_db), _=Depends(get_current_staff)):
    """Lock a card. Balance on the account is NOT affected."""
    card = db.query(Card).filter(Card.id == card_id, Card.student_id == student_id).first()
    if not card:
        raise HTTPException(status_code=404, detail={"code": "CARD_NOT_FOUND", "message": "Card not found for this student."})
    if card.status == CardStatus.locked:
        raise HTTPException(status_code=409, detail={"code": "ALREADY_LOCKED", "message": "Card is already locked."})
    from datetime import datetime
    card.status = CardStatus.locked
    card.locked_at = datetime.utcnow()
    db.commit()
    return OkResponse()


@router.post("/{student_id}/cards", response_model=CardOut)
def issue_card(student_id: int, card_token: str, db: Session = Depends(get_db), _=Depends(get_current_staff)):
    """Issue a replacement card. Previous active card must be locked first."""
    student = _get_or_404(db, student_id)
    active = next((c for c in student.cards if c.status == CardStatus.active), None)
    if active:
        raise HTTPException(
            status_code=409,
            detail={"code": "ACTIVE_CARD_EXISTS", "message": "Student already has an active card. Lock it before issuing a replacement."},
        )
    card = Card(student_id=student_id, card_token=card_token, status=CardStatus.active)
    db.add(card)
    db.commit()
    db.refresh(card)
    return card


def _get_or_404(db: Session, student_id: int) -> Student:
    student = db.get(Student, student_id)
    if not student:
        raise HTTPException(status_code=404, detail={"code": "STUDENT_NOT_FOUND", "message": "Student not found."})
    return student
