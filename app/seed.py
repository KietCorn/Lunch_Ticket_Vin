"""
Seed demo data. Run once after creating tables:
    python -m app.seed
"""
import hashlib
from datetime import date, datetime
from app.database import SessionLocal, engine
from app.models import Base, Student, Card, Account, Staff, MenuItem, Timeslot, DailyMenu, CardStatus


def _hash(password: str) -> str:
    return hashlib.sha256(password.encode()).hexdigest()


def seed():
    Base.metadata.create_all(bind=engine)
    db = SessionLocal()

    if db.query(Student).count() > 0:
        print("Database already seeded — skipping.")
        db.close()
        return

    today = date.today().isoformat()

    # ── Staff ─────────────────────────────────────────────────────────────────
    staff_members = [
        Staff(username="admin", full_name="Nguyễn Admin", password_hash=_hash("admin123"), is_admin=True),
        Staff(username="staff1", full_name="Trần Bình", password_hash=_hash("staff123"), is_admin=False),
        Staff(username="staff2", full_name="Lê Hương", password_hash=_hash("staff123"), is_admin=False),
    ]
    db.add_all(staff_members)
    db.flush()

    # ── Students + Cards + Accounts ───────────────────────────────────────────
    students_data = [
        ("20110001", "Nguyễn Văn A", "a@stu.edu.vn",  "A0012", 125_000),
        ("20110002", "Lê Thị B",     "b@stu.edu.vn",  "B0034",  85_000),
        ("20110003", "Phạm Thị C",   "c@stu.edu.vn",  "C0056", 200_000),
        ("20110004", "Trần Văn D",   "d@stu.edu.vn",  "D0078",  50_000),
        ("20110005", "Vũ Thị E",     "e@stu.edu.vn",  "E0090", 170_000),
        ("20110006", "Hoàng Văn F",  "f@stu.edu.vn",  "F0011", 310_000),
        ("20110007", "Đặng Thị G",   "g@stu.edu.vn",  "G0022",  35_000),
        ("20110008", "Bùi Văn H",    "h@stu.edu.vn",  "H0033", 140_000),
        ("20110009", "Ngô Thị I",    "i@stu.edu.vn",  "I0044",  90_000),
        ("20110010", "Đinh Văn K",   "k@stu.edu.vn",  "K0055", 255_000),
    ]
    students = []
    for mssv, name, email, token, balance in students_data:
        s = Student(student_id=mssv, full_name=name, email=email)
        db.add(s)
        db.flush()
        db.add(Card(student_id=s.id, card_token=token, status=CardStatus.active))
        db.add(Account(student_id=s.id, balance=balance))
        students.append(s)
    db.flush()

    # ── Menu items ────────────────────────────────────────────────────────────
    items = [
        MenuItem(name="Bún bò Huế",     description="Bún, thịt bò, chả, rau sống",     price=30_000),
        MenuItem(name="Cơm sườn nướng", description="Cơm, sườn, canh chua, rau",        price=35_000),
        MenuItem(name="Phở bò tái",     description="Phở, thịt bò tái, hành, gừng",     price=35_000),
        MenuItem(name="Cơm gà nướng",   description="Cơm, đùi gà, salad, nước mắm",     price=35_000),
        MenuItem(name="Mì xào bò",      description="Mì, thịt bò, hành tây, rau củ",    price=30_000),
    ]
    db.add_all(items)
    db.flush()

    # ── Timeslots ─────────────────────────────────────────────────────────────
    slots = [
        Timeslot(label="10:30 – 11:00", start_time="10:30", end_time="11:00", sort_order=1),
        Timeslot(label="11:00 – 11:30", start_time="11:00", end_time="11:30", sort_order=2),
        Timeslot(label="11:30 – 12:00", start_time="11:30", end_time="12:00", sort_order=3),
        Timeslot(label="12:00 – 12:30", start_time="12:00", end_time="12:30", sort_order=4),
    ]
    db.add_all(slots)
    db.flush()

    # ── Daily menu for today ──────────────────────────────────────────────────
    # Each item available in each slot with varying quantities
    menu_entries = [
        # slot 1 (10:30–11:00)
        DailyMenu(date=today, timeslot_id=slots[0].id, menu_item_id=items[0].id, available_quantity=20),
        DailyMenu(date=today, timeslot_id=slots[0].id, menu_item_id=items[1].id, available_quantity=15),
        # slot 2 (11:00–11:30)
        DailyMenu(date=today, timeslot_id=slots[1].id, menu_item_id=items[0].id, available_quantity=18),
        DailyMenu(date=today, timeslot_id=slots[1].id, menu_item_id=items[1].id, available_quantity=3),
        DailyMenu(date=today, timeslot_id=slots[1].id, menu_item_id=items[2].id, available_quantity=0),
        DailyMenu(date=today, timeslot_id=slots[1].id, menu_item_id=items[3].id, available_quantity=12),
        # slot 3 (11:30–12:00)
        DailyMenu(date=today, timeslot_id=slots[2].id, menu_item_id=items[0].id, available_quantity=25),
        DailyMenu(date=today, timeslot_id=slots[2].id, menu_item_id=items[3].id, available_quantity=20),
        DailyMenu(date=today, timeslot_id=slots[2].id, menu_item_id=items[4].id, available_quantity=15),
        # slot 4 (12:00–12:30)
        DailyMenu(date=today, timeslot_id=slots[3].id, menu_item_id=items[1].id, available_quantity=10),
        DailyMenu(date=today, timeslot_id=slots[3].id, menu_item_id=items[4].id, available_quantity=8),
    ]
    db.add_all(menu_entries)

    db.commit()
    print(f"Seeded: {len(students)} students, {len(items)} menu items, {len(slots)} timeslots, {len(menu_entries)} daily menu entries.")
    print("Staff logins — admin: admin/admin123  |  staff: staff1/staff123")
    db.close()


if __name__ == "__main__":
    seed()
