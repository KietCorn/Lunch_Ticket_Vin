from datetime import date as date_type
from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session, joinedload
from app.database import get_db
from app.models import DailyMenu, Timeslot, MenuItem
from app.schemas import DailyMenuOut, TimeslotOut
from app.auth import get_current_staff

router = APIRouter(prefix="/api/menu", tags=["menu"])


@router.get("", response_model=list[DailyMenuOut])
def get_menu(
    date: str = Query(default=None, description="ISO date YYYY-MM-DD; defaults to today"),
    db: Session = Depends(get_db),
    _=Depends(get_current_staff),
):
    target = date or date_type.today().isoformat()
    entries = (
        db.query(DailyMenu)
        .options(joinedload(DailyMenu.timeslot), joinedload(DailyMenu.menu_item))
        .filter(DailyMenu.date == target)
        .order_by(DailyMenu.timeslot_id)
        .all()
    )
    return entries


@router.get("/timeslots", response_model=list[TimeslotOut])
def list_timeslots(db: Session = Depends(get_db), _=Depends(get_current_staff)):
    return db.query(Timeslot).order_by(Timeslot.sort_order).all()
