-- LunchCard – Database Schema
-- Auto-generated from app/models.py (SQLAlchemy is the source of truth).
-- Regenerate after any model change — do not hand-edit.
--
-- Regenerate with:
--   python3 -c "
--   from sqlalchemy.schema import CreateTable
--   from sqlalchemy import create_engine
--   from app.models import Base
--   engine = create_engine('sqlite:///:memory:')
--   for t in Base.metadata.sorted_tables:
--       print(str(CreateTable(t).compile(engine)).strip() + ';\n')
--   "

CREATE TABLE menu_items (
	id INTEGER NOT NULL,
	name VARCHAR(100) NOT NULL,
	description TEXT,
	price NUMERIC(10, 0) NOT NULL,
	is_active BOOLEAN NOT NULL,
	PRIMARY KEY (id)
);

CREATE TABLE staff (
	id INTEGER NOT NULL,
	username VARCHAR(50) NOT NULL,
	full_name VARCHAR(100) NOT NULL,
	password_hash VARCHAR(200) NOT NULL,
	is_admin BOOLEAN NOT NULL,
	is_active BOOLEAN NOT NULL,
	created_at DATETIME NOT NULL,
	PRIMARY KEY (id),
	UNIQUE (username)
);

CREATE TABLE students (
	id INTEGER NOT NULL,
	student_id VARCHAR(20) NOT NULL,
	full_name VARCHAR(100) NOT NULL,
	email VARCHAR(100),
	created_at DATETIME NOT NULL,
	updated_at DATETIME,
	PRIMARY KEY (id),
	UNIQUE (student_id),
	UNIQUE (email)
);

CREATE TABLE timeslots (
	id INTEGER NOT NULL,
	label VARCHAR(20) NOT NULL,
	start_time VARCHAR(5) NOT NULL,
	end_time VARCHAR(5) NOT NULL,
	sort_order INTEGER NOT NULL,
	PRIMARY KEY (id)
);

CREATE TABLE accounts (
	id INTEGER NOT NULL,
	student_id INTEGER NOT NULL,
	balance NUMERIC(10, 0) NOT NULL,
	updated_at DATETIME,
	PRIMARY KEY (id),
	CONSTRAINT ck_balance_non_negative CHECK (balance >= 0),
	UNIQUE (student_id),
	FOREIGN KEY(student_id) REFERENCES students (id)
);

CREATE TABLE cards (
	id INTEGER NOT NULL,
	student_id INTEGER NOT NULL,
	card_token VARCHAR(50) NOT NULL,
	status VARCHAR(6) NOT NULL,
	issued_at DATETIME NOT NULL,
	locked_at DATETIME,
	PRIMARY KEY (id),
	FOREIGN KEY(student_id) REFERENCES students (id),
	UNIQUE (card_token)
);

CREATE TABLE daily_menu (
	id INTEGER NOT NULL,
	date VARCHAR(10) NOT NULL,
	timeslot_id INTEGER NOT NULL,
	menu_item_id INTEGER NOT NULL,
	available_quantity INTEGER NOT NULL,
	PRIMARY KEY (id),
	CONSTRAINT uq_daily_menu_slot_item UNIQUE (date, timeslot_id, menu_item_id),
	CONSTRAINT ck_available_quantity_non_negative CHECK (available_quantity >= 0),
	FOREIGN KEY(timeslot_id) REFERENCES timeslots (id),
	FOREIGN KEY(menu_item_id) REFERENCES menu_items (id)
);

CREATE TABLE orders (
	id INTEGER NOT NULL,
	student_id INTEGER NOT NULL,
	daily_menu_id INTEGER NOT NULL,
	timeslot_id INTEGER NOT NULL,
	order_type VARCHAR(9) NOT NULL,
	status VARCHAR(9) NOT NULL,
	source VARCHAR(12),
	amount_charged NUMERIC(10, 0) NOT NULL,
	qr_token VARCHAR(50),
	qr_expires_at DATETIME,
	placed_by_staff INTEGER,
	delivered_by INTEGER,
	created_at DATETIME NOT NULL,
	updated_at DATETIME,
	PRIMARY KEY (id),
	FOREIGN KEY(student_id) REFERENCES students (id),
	FOREIGN KEY(daily_menu_id) REFERENCES daily_menu (id),
	FOREIGN KEY(timeslot_id) REFERENCES timeslots (id),
	UNIQUE (qr_token),
	FOREIGN KEY(placed_by_staff) REFERENCES staff (id),
	FOREIGN KEY(delivered_by) REFERENCES staff (id)
);

CREATE TABLE transactions (
	id INTEGER NOT NULL,
	account_id INTEGER NOT NULL,
	transaction_type VARCHAR(9) NOT NULL,
	amount NUMERIC(10, 0) NOT NULL,
	balance_after NUMERIC(10, 0) NOT NULL,
	reference_order INTEGER,
	actor_id INTEGER,
	note TEXT,
	created_at DATETIME NOT NULL,
	PRIMARY KEY (id),
	FOREIGN KEY(account_id) REFERENCES accounts (id),
	FOREIGN KEY(reference_order) REFERENCES orders (id),
	FOREIGN KEY(actor_id) REFERENCES staff (id)
);
