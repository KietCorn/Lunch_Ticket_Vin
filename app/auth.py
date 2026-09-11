"""
Minimal session-based auth for the demo.
Sessions are stored in memory — intentionally; this is a local demo, not production.
"""
import secrets
from typing import Optional
from fastapi import Cookie, Depends, HTTPException
from sqlalchemy.orm import Session
from app.database import get_db
from app.models import Staff

# In-memory session store: token → staff_id
_sessions: dict[str, int] = {}


def create_session(staff_id: int) -> str:
    token = secrets.token_urlsafe(32)
    _sessions[token] = staff_id
    return token


def delete_session(token: str) -> None:
    _sessions.pop(token, None)


def get_current_staff(
    session_token: Optional[str] = Cookie(default=None),
    db: Session = Depends(get_db),
) -> Staff:
    if not session_token or session_token not in _sessions:
        raise HTTPException(status_code=401, detail={"code": "UNAUTHORIZED", "message": "Not logged in."})
    staff = db.get(Staff, _sessions[session_token])
    if not staff or not staff.is_active:
        raise HTTPException(status_code=401, detail={"code": "UNAUTHORIZED", "message": "Staff account not found or inactive."})
    return staff


def require_admin(staff: Staff = Depends(get_current_staff)) -> Staff:
    if not staff.is_admin:
        raise HTTPException(status_code=403, detail={"code": "FORBIDDEN", "message": "Admin access required."})
    return staff
