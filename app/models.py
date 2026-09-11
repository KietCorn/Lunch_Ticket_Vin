"""
SQLAlchemy ORM models for the student lunch card system.

Design constraints enforced here:
  - Balance lives on Account, never on Card (BR-02)
  - Card lock/replace never touches Account.balance (BR-07)
  - Transactions is append-only (no update/delete in application code)
  - available_quantity cannot go below 0 (BR-05)
  - orders.order_type is set at creation and never changed
"""

from datetime import datetime
from enum import Enum as PyEnum

from sqlalchemy import (
    Boolean, CheckConstraint, Column, DateTime, Enum, ForeignKey,
    Integer, Numeric, String, Text, UniqueConstraint,
)
from sqlalchemy.orm import DeclarativeBase, relationship


class Base(DeclarativeBase):
    pass


# ── Enums ────────────────────────────────────────────────────────────────────

class CardStatus(str, PyEnum):
    active = "active"
    locked = "locked"


class OrderType(str, PyEnum):
    pre_order = "pre_order"
    walk_in   = "walk_in"


class OrderStatus(str, PyEnum):
    pending   = "pending"    # placed, not yet ready
    ready     = "ready"      # kitchen marked ready, awaiting pickup
    delivered = "delivered"  # staff confirmed delivery
    cancelled = "cancelled"  # cancelled before delivery


class TransactionType(str, PyEnum):
    top_up    = "top_up"     # admin/staff adds money
    deduction = "deduction"  # order confirmed, money deducted
    refund    = "refund"     # order cancelled after deduction


class OrderSource(str, PyEnum):
    qr_scan      = "qr_scan"       # staff scanned QR code
    manual_entry = "manual_entry"  # staff typed student ID (offline fallback)


# ── Core identity ─────────────────────────────────────────────────────────────

class Student(Base):
    """
    Identity record for a student.
    In a real deployment this would sync from the school's REST API;
    for the demo, populated via seed data.
    """
    __tablename__ = "students"

    id         = Column(Integer, primary_key=True)
    student_id = Column(String(20), unique=True, nullable=False)  # e.g. "20110001"
    full_name  = Column(String(100), nullable=False)
    email      = Column(String(100), unique=True, nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow, nullable=False)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)

    account = relationship("Account", back_populates="student", uselist=False)
    cards   = relationship("Card",    back_populates="student")
    orders  = relationship("Order",   back_populates="student")


class Card(Base):
    """
    Physical card token. A student may have multiple rows (history of issued
    cards), but only one may be active at a time.

    Locking or replacing a card MUST NOT touch Account.balance — this table
    is the only thing that changes during a card swap.
    """
    __tablename__ = "cards"
    __table_args__ = (
        # Only one active card per student at a time
        UniqueConstraint("student_id", "status",
                         name="uq_one_active_card_per_student",
                         sqlite_where="status = 'active'"),
    )

    id         = Column(Integer, primary_key=True)
    student_id = Column(Integer, ForeignKey("students.id"), nullable=False)
    card_token = Column(String(50), unique=True, nullable=False)  # physical card identifier
    status     = Column(Enum(CardStatus), default=CardStatus.active, nullable=False)
    issued_at  = Column(DateTime, default=datetime.utcnow, nullable=False)
    locked_at  = Column(DateTime, nullable=True)

    student = relationship("Student", back_populates="cards")


# ── Financial ─────────────────────────────────────────────────────────────────

class Account(Base):
    """
    Balance ledger. Belongs to a student, not a card.
    balance must never go below 0 — enforced by CHECK constraint AND
    application-level guard inside a transaction (lunchcard-db rule).
    """
    __tablename__ = "accounts"
    __table_args__ = (
        CheckConstraint("balance >= 0", name="ck_balance_non_negative"),
    )

    id         = Column(Integer, primary_key=True)
    student_id = Column(Integer, ForeignKey("students.id"), unique=True, nullable=False)
    balance    = Column(Numeric(10, 0), default=0, nullable=False)  # VND, integer cents
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)

    student      = relationship("Student", back_populates="account")
    transactions = relationship("Transaction", back_populates="account")


class Transaction(Base):
    """
    Append-only audit log. Every balance change writes one row here atomically.
    No UPDATE or DELETE on this table in application code.
    """
    __tablename__ = "transactions"

    id               = Column(Integer, primary_key=True)
    account_id       = Column(Integer, ForeignKey("accounts.id"), nullable=False)
    transaction_type = Column(Enum(TransactionType), nullable=False)
    amount           = Column(Numeric(10, 0), nullable=False)  # positive = credit, negative = debit
    balance_after    = Column(Numeric(10, 0), nullable=False)  # snapshot for audit
    reference_order  = Column(Integer, ForeignKey("orders.id"), nullable=True)  # null for top-ups
    actor_id         = Column(Integer, ForeignKey("staff.id"), nullable=True)   # who performed the top-up
    note             = Column(Text, nullable=True)
    created_at       = Column(DateTime, default=datetime.utcnow, nullable=False)

    account = relationship("Account", back_populates="transactions")
    order   = relationship("Order",   back_populates="transactions")


# ── Menu ──────────────────────────────────────────────────────────────────────

class MenuItem(Base):
    """Master catalog of food items."""
    __tablename__ = "menu_items"

    id          = Column(Integer, primary_key=True)
    name        = Column(String(100), nullable=False)
    description = Column(Text, nullable=True)
    price       = Column(Numeric(10, 0), nullable=False)  # VND
    is_active   = Column(Boolean, default=True, nullable=False)

    daily_entries = relationship("DailyMenu", back_populates="menu_item")


class Timeslot(Base):
    """Named pickup windows (e.g., 11:00–11:30). Reused across days."""
    __tablename__ = "timeslots"

    id         = Column(Integer, primary_key=True)
    label      = Column(String(20), nullable=False)   # "11:00 – 11:30"
    start_time = Column(String(5),  nullable=False)   # "11:00" — stored as HH:MM string for SQLite
    end_time   = Column(String(5),  nullable=False)   # "11:30"
    sort_order = Column(Integer,    nullable=False)

    daily_menu = relationship("DailyMenu", back_populates="timeslot")
    orders     = relationship("Order",     back_populates="timeslot")


class DailyMenu(Base):
    """
    Which items are served on a given date + timeslot, and how many slots remain.

    available_quantity is decremented atomically when an order is confirmed.
    CHECK constraint is the last-resort guard; application code must use
    SELECT ... FOR UPDATE (or equivalent) before decrementing.
    """
    __tablename__ = "daily_menu"
    __table_args__ = (
        UniqueConstraint("date", "timeslot_id", "menu_item_id", name="uq_daily_menu_slot_item"),
        CheckConstraint("available_quantity >= 0", name="ck_available_quantity_non_negative"),
    )

    id                 = Column(Integer, primary_key=True)
    date               = Column(String(10), nullable=False)   # "2026-09-11" ISO date string
    timeslot_id        = Column(Integer, ForeignKey("timeslots.id"), nullable=False)
    menu_item_id       = Column(Integer, ForeignKey("menu_items.id"), nullable=False)
    available_quantity = Column(Integer, nullable=False)
    total_quantity     = Column(Integer, nullable=False)       # original capacity, for reporting

    timeslot  = relationship("Timeslot",  back_populates="daily_menu")
    menu_item = relationship("MenuItem",  back_populates="daily_entries")
    orders    = relationship("Order",     back_populates="daily_menu_entry")


# ── Orders ────────────────────────────────────────────────────────────────────

class Order(Base):
    """
    One order per student per timeslot per day.
    order_type is set at creation and MUST NOT be changed after the fact.
    source records whether staff used QR scan or manual entry (offline fallback).
    """
    __tablename__ = "orders"
    __table_args__ = (
        UniqueConstraint("student_id", "daily_menu_id", name="uq_one_order_per_student_per_slot"),
    )

    id             = Column(Integer, primary_key=True)
    student_id     = Column(Integer, ForeignKey("students.id"),    nullable=False)
    daily_menu_id  = Column(Integer, ForeignKey("daily_menu.id"),  nullable=False)
    timeslot_id    = Column(Integer, ForeignKey("timeslots.id"),   nullable=False)
    order_type     = Column(Enum(OrderType),   nullable=False)   # pre_order | walk_in — immutable
    status         = Column(Enum(OrderStatus), default=OrderStatus.pending, nullable=False)
    source         = Column(Enum(OrderSource), nullable=True)    # set when staff confirms delivery
    amount_charged = Column(Numeric(10, 0),    nullable=False)
    qr_token       = Column(String(50), unique=True, nullable=True)   # null for walk-in
    qr_expires_at  = Column(DateTime,   nullable=True)                # null for walk-in
    placed_by_staff= Column(Integer, ForeignKey("staff.id"), nullable=True)  # null for pre-orders
    delivered_by   = Column(Integer, ForeignKey("staff.id"), nullable=True)
    created_at     = Column(DateTime, default=datetime.utcnow, nullable=False)
    updated_at     = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)

    student         = relationship("Student",   back_populates="orders")
    daily_menu_entry= relationship("DailyMenu", back_populates="orders")
    timeslot        = relationship("Timeslot",  back_populates="orders")
    transactions    = relationship("Transaction", back_populates="order")


# ── Staff ─────────────────────────────────────────────────────────────────────

class Staff(Base):
    """
    Counter staff and admin accounts.
    Separate from Student — different auth, different permissions.
    """
    __tablename__ = "staff"

    id           = Column(Integer, primary_key=True)
    username     = Column(String(50), unique=True, nullable=False)
    full_name    = Column(String(100), nullable=False)
    password_hash= Column(String(200), nullable=False)
    is_admin     = Column(Boolean, default=False, nullable=False)
    is_active    = Column(Boolean, default=True,  nullable=False)
    created_at   = Column(DateTime, default=datetime.utcnow, nullable=False)
