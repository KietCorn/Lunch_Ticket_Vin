from __future__ import annotations
from datetime import datetime
from typing import Optional
from pydantic import BaseModel, field_validator
from app.models import CardStatus, OrderType, OrderStatus, TransactionType, OrderSource


# ── Shared ────────────────────────────────────────────────────────────────────

class OkResponse(BaseModel):
    ok: bool = True


# ── Students ──────────────────────────────────────────────────────────────────

class StudentOut(BaseModel):
    id: int
    student_id: str
    full_name: str
    email: Optional[str]

    model_config = {"from_attributes": True}


# ── Cards ─────────────────────────────────────────────────────────────────────

class CardOut(BaseModel):
    id: int
    card_token: str
    status: CardStatus
    issued_at: datetime

    model_config = {"from_attributes": True}


# ── Accounts & Transactions ───────────────────────────────────────────────────

class AccountOut(BaseModel):
    id: int
    balance: int
    student: StudentOut

    model_config = {"from_attributes": True}

    @field_validator("balance", mode="before")
    @classmethod
    def coerce_decimal(cls, v):
        return int(v)


class TransactionOut(BaseModel):
    id: int
    transaction_type: TransactionType
    amount: int
    balance_after: int
    reference_order: Optional[int] = None    # matches Transaction.reference_order column name
    note: Optional[str] = None
    created_at: datetime

    model_config = {"from_attributes": True}

    @field_validator("amount", "balance_after", mode="before")
    @classmethod
    def coerce_decimal(cls, v):
        return int(v)


class TopUpRequest(BaseModel):
    amount: int      # VND, must be positive
    note: Optional[str] = None
    staff_id: int


class TopUpResponse(BaseModel):
    transaction: TransactionOut
    new_balance: int


# ── Menu ──────────────────────────────────────────────────────────────────────

class TimeslotOut(BaseModel):
    id: int
    label: str
    start_time: str
    end_time: str
    sort_order: int

    model_config = {"from_attributes": True}


class MenuItemOut(BaseModel):
    id: int
    name: str
    description: Optional[str]
    price: int

    model_config = {"from_attributes": True}

    @field_validator("price", mode="before")
    @classmethod
    def coerce_decimal(cls, v):
        return int(v)


class DailyMenuOut(BaseModel):
    id: int
    date: str
    timeslot: TimeslotOut
    menu_item: MenuItemOut
    available_quantity: int

    model_config = {"from_attributes": True}


# ── Orders ────────────────────────────────────────────────────────────────────

class OrderOut(BaseModel):
    id: int
    order_type: OrderType
    status: OrderStatus
    amount_charged: int
    qr_token: Optional[str] = None
    qr_expires_at: Optional[datetime] = None
    created_at: datetime
    student: StudentOut
    timeslot: TimeslotOut
    item_name: str           # resolved in route handler

    model_config = {"from_attributes": True}

    @field_validator("amount_charged", mode="before")
    @classmethod
    def coerce_decimal(cls, v):
        return int(v)


class PreOrderRequest(BaseModel):
    student_id: int
    daily_menu_id: int


class WalkInRequest(BaseModel):
    student_id: int
    daily_menu_id: int
    staff_id: int


class ConfirmDeliveryRequest(BaseModel):
    source: OrderSource
    staff_id: int


# ── Kitchen (SSE payload) ─────────────────────────────────────────────────────

class KitchenOrderOut(BaseModel):
    id: int
    order_type: OrderType
    status: OrderStatus
    student_name: str
    item_name: str
    timeslot_label: str
    created_at: str          # "HH:MM" for display


# ── Staff / Auth ──────────────────────────────────────────────────────────────

class StaffOut(BaseModel):
    id: int
    username: str
    full_name: str
    is_admin: bool

    model_config = {"from_attributes": True}


class LoginRequest(BaseModel):
    username: str
    password: str


class LoginResponse(BaseModel):
    session_token: str
    staff: StaffOut
