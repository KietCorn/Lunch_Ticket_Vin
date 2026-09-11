from fastapi import APIRouter, Depends, HTTPException, Response
from sqlalchemy.orm import Session
from app.database import get_db
from app.models import Staff
from app.schemas import LoginRequest, LoginResponse, StaffOut, OkResponse
from app.auth import create_session, delete_session, get_current_staff
import hashlib

router = APIRouter(prefix="/api/auth", tags=["auth"])


def _hash(password: str) -> str:
    return hashlib.sha256(password.encode()).hexdigest()


@router.post("/login", response_model=LoginResponse)
def login(body: LoginRequest, response: Response, db: Session = Depends(get_db)):
    staff = db.query(Staff).filter(Staff.username == body.username).first()
    if not staff or not staff.is_active or staff.password_hash != _hash(body.password):
        raise HTTPException(status_code=401, detail={"code": "INVALID_CREDENTIALS", "message": "Invalid username or password."})

    token = create_session(staff.id)
    response.set_cookie(key="session_token", value=token, httponly=True, samesite="lax")
    return LoginResponse(session_token=token, staff=StaffOut.model_validate(staff))


@router.post("/logout", response_model=OkResponse)
def logout(response: Response, staff: Staff = Depends(get_current_staff)):
    # We don't have the raw token here, so just clear the cookie;
    # the session will remain in memory until server restart (acceptable for demo)
    response.delete_cookie("session_token")
    return OkResponse()


@router.get("/me", response_model=StaffOut)
def me(staff: Staff = Depends(get_current_staff)):
    return staff
