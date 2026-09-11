/* Student frontend — state machine over 6 screens */

const API = "http://localhost:8000/api";

// ── State ─────────────────────────────────────────────────────────────────────

const state = {
  sessionToken: localStorage.getItem("session_token"),
  staff: JSON.parse(localStorage.getItem("staff") || "null"),
  student: null,      // looked up after login (demo: hardcoded student_id=1)
  account: null,
  menu: [],
  orders: [],
  selectedEntry: null,  // DailyMenuOut selected for pre-order
  placedOrder: null,    // OrderOut after successful pre-order
};

// ── API helpers ───────────────────────────────────────────────────────────────

async function api(method, path, body) {
  const res = await fetch(`${API}${path}`, {
    method,
    credentials: "include",
    headers: body ? { "Content-Type": "application/json" } : {},
    body: body ? JSON.stringify(body) : undefined,
  });
  const data = await res.json();
  if (!res.ok) throw { status: res.status, ...data.detail };
  return data;
}

// ── Screen routing ────────────────────────────────────────────────────────────

function show(screenId) {
  document.querySelectorAll(".screen").forEach(s => s.classList.remove("active"));
  document.getElementById(screenId).classList.add("active");
}

function setNav(tab) {
  document.querySelectorAll(".nav-item").forEach(n => n.classList.remove("active"));
  document.querySelectorAll(`.nav-item[data-tab="${tab}"]`).forEach(n => n.classList.add("active"));
}

function showError(msg) {
  const el = document.getElementById("global-error");
  el.textContent = msg;
  el.style.display = "block";
  setTimeout(() => { el.style.display = "none"; }, 4000);
}

// ── Login ─────────────────────────────────────────────────────────────────────

async function login(username, password) {
  const data = await api("POST", "/auth/login", { username, password });
  state.sessionToken = data.session_token;
  state.staff = data.staff;
  localStorage.setItem("session_token", data.session_token);
  localStorage.setItem("staff", JSON.stringify(data.staff));

  // Demo: always load student MSSV 20110001 as the "logged-in student"
  await loadStudent("20110001");
  await loadDashboard();
}

async function loadStudent(mssv) {
  state.student = await api("GET", `/students/lookup?q=${mssv}`);
  state.account  = await api("GET", `/students/${state.student.id}/account`);
}

// ── Dashboard ─────────────────────────────────────────────────────────────────

async function loadDashboard() {
  renderDashboard();
  show("screen-dashboard");
  setNav("home");

  // Load today's orders and a menu preview in parallel
  const [orders, menu] = await Promise.all([
    api("GET", `/orders?student_id=${state.student.id}`),
    api("GET", "/menu"),
  ]);
  state.orders = orders;
  state.menu   = menu;
  renderDashboard();
}

function renderDashboard() {
  const { student, account, orders, menu } = state;
  if (!student) return;

  document.getElementById("dash-name").textContent    = student.full_name;
  document.getElementById("dash-balance").textContent = fmt(account?.balance ?? 0);
  document.getElementById("dash-mssv").textContent    = `MSSV: ${student.student_id}`;

  // Today's first pending/ready order
  const activeOrder = orders.find(o => ["pending","ready"].includes(o.status));
  const orderEl = document.getElementById("dash-active-order");
  if (activeOrder) {
    orderEl.innerHTML = `
      <div class="order-item" data-id="${activeOrder.id}">
        <span class="order-icon">🍜</span>
        <div class="order-info">
          <div class="order-name">${activeOrder.item_name}</div>
          <div class="order-meta">⏰ ${activeOrder.timeslot.label} · ${activeOrder.order_type === "pre_order" ? "Pre-order" : "Walk-in"}</div>
          <div style="margin-top:4px;">${statusBadge(activeOrder.status)}</div>
        </div>
        <div class="order-price">${fmt(activeOrder.amount_charged)}</div>
      </div>`;
    orderEl.querySelector(".order-item").addEventListener("click", () => openQR(activeOrder));
  } else {
    orderEl.innerHTML = `<div style="padding:12px;text-align:center;color:var(--gray-400);font-size:13px;">No active orders today</div>`;
  }

  // Menu preview (first 2 available entries)
  const available = menu.filter(e => e.available_quantity > 0).slice(0, 2);
  document.getElementById("dash-menu-preview").innerHTML = available.map(e => `
    <div class="order-item">
      <span class="order-icon">🍽️</span>
      <div class="order-info">
        <div class="order-name">${e.menu_item.name}</div>
        <div class="order-meta">Còn ${e.available_quantity} suất · ${e.timeslot.label}</div>
      </div>
      <div style="text-align:right;">
        <div class="order-price">${fmt(e.menu_item.price)}</div>
        <button class="btn btn-primary btn-sm" style="margin-top:4px;" data-entry-id="${e.id}">Đặt</button>
      </div>
    </div>`).join("") || `<div style="padding:12px;text-align:center;color:var(--gray-400);font-size:13px;">No items available</div>`;

  document.querySelectorAll("[data-entry-id]").forEach(btn => {
    btn.addEventListener("click", e => {
      const entry = state.menu.find(m => m.id === parseInt(btn.dataset.entryId));
      startPreOrder(entry);
    });
  });
}

// ── Menu screen ───────────────────────────────────────────────────────────────

async function loadMenu() {
  show("screen-menu");
  setNav("menu");
  if (!state.menu.length) state.menu = await api("GET", "/menu");
  renderMenu();
}

function renderMenu(filterTimeslotId = null) {
  const { menu } = state;

  // Build timeslot tabs
  const slots = [...new Map(menu.map(e => [e.timeslot.id, e.timeslot])).values()]
    .sort((a,b) => a.sort_order - b.sort_order);

  const activeSlot = filterTimeslotId ?? slots[0]?.id;
  document.getElementById("menu-timeslot-tabs").innerHTML = slots.map(s => `
    <div class="time-tab ${s.id === activeSlot ? "active" : ""}" data-slot="${s.id}">
      ${s.start_time}
    </div>`).join("");

  document.querySelectorAll(".time-tab").forEach(tab => {
    tab.addEventListener("click", () => renderMenu(parseInt(tab.dataset.slot)));
  });

  const filtered = menu.filter(e => e.timeslot.id === activeSlot);
  document.getElementById("menu-items").innerHTML = filtered.map(e => {
    const soldOut = e.available_quantity === 0;
    const low     = !soldOut && e.available_quantity <= 5;
    return `
      <div class="menu-item ${soldOut ? "sold-out" : ""}">
        <div class="menu-img">${itemEmoji(e.menu_item.name)}</div>
        <div class="menu-info">
          <div>
            <div class="menu-name">${e.menu_item.name}</div>
            <div class="menu-desc">${e.menu_item.description || ""}</div>
          </div>
          <div class="menu-footer">
            <div>
              <div class="menu-price">${fmt(e.menu_item.price)}</div>
              <div style="font-size:10px;margin-top:1px;color:${soldOut ? "var(--gray-400)" : low ? "var(--danger)" : "var(--gray-500)"}">
                ${soldOut ? "Hết suất" : `Còn ${e.available_quantity} suất`}
              </div>
            </div>
            <div class="add-btn ${soldOut ? "disabled" : ""}" data-entry-id="${e.id}">+</div>
          </div>
        </div>
      </div>`;
  }).join("");

  document.querySelectorAll(".add-btn:not(.disabled)").forEach(btn => {
    btn.addEventListener("click", () => {
      const entry = state.menu.find(e => e.id === parseInt(btn.dataset.entryId));
      startPreOrder(entry);
    });
  });
}

// ── Pre-order flow ────────────────────────────────────────────────────────────

function startPreOrder(entry) {
  state.selectedEntry = entry;
  renderPreOrderStep2(entry);
  show("screen-preorder");
}

function renderPreOrderStep2(entry) {
  document.getElementById("po-item-name").textContent  = entry.menu_item.name;
  document.getElementById("po-item-desc").textContent  = entry.menu_item.description || "";
  document.getElementById("po-item-price").textContent = fmt(entry.menu_item.price);

  // Build timeslot grid from entries with same menu item available today
  const sameItem = state.menu.filter(e => e.menu_item.id === entry.menu_item.id);
  document.getElementById("po-timeslot-grid").innerHTML = sameItem.map(e => {
    const full = e.available_quantity === 0;
    const low  = !full && e.available_quantity <= 5;
    const selected = e.id === entry.id;
    return `
      <div class="time-slot ${selected ? "selected" : ""} ${full ? "full" : ""}"
           data-entry-id="${e.id}" ${full ? "" : ""}>
        <div class="slot-time">${e.timeslot.start_time}</div>
        <div class="slot-avail ${low ? "low" : full ? "full-text" : ""}">
          ${full ? "Hết chỗ" : `Còn ${e.available_quantity}`}
        </div>
      </div>`;
  }).join("");

  document.querySelectorAll(".time-slot:not(.full)").forEach(slot => {
    slot.addEventListener("click", () => {
      const newEntry = state.menu.find(e => e.id === parseInt(slot.dataset.entryId));
      state.selectedEntry = newEntry;
      renderPreOrderStep2(newEntry);
    });
  });

  updatePreOrderSummary(entry);
}

function updatePreOrderSummary(entry) {
  const price  = entry.menu_item.price;
  const before = state.account?.balance ?? 0;
  const after  = before - price;
  document.getElementById("po-summary-item").textContent   = `${entry.menu_item.name} × 1`;
  document.getElementById("po-summary-price").textContent  = fmt(price);
  document.getElementById("po-balance-before").textContent = fmt(before);
  document.getElementById("po-balance-after").textContent  = after >= 0 ? fmt(after) : "Không đủ số dư";
  document.getElementById("po-balance-after").style.color  = after >= 0 ? "var(--success)" : "var(--danger)";
  document.getElementById("po-confirm-btn").disabled       = after < 0;
}

async function submitPreOrder() {
  const btn = document.getElementById("po-confirm-btn");
  btn.disabled = true;
  btn.textContent = "Đang xử lý…";
  try {
    const order = await api("POST", "/orders/preorder", {
      student_id: state.student.id,
      daily_menu_id: state.selectedEntry.id,
    });
    state.placedOrder = order;
    // Refresh account balance
    state.account = await api("GET", `/students/${state.student.id}/account`);
    openQR(order);
  } catch (err) {
    showError(err.message || "Đặt món thất bại.");
    btn.disabled = false;
    btn.textContent = "Xác nhận đặt món →";
  }
}

// ── QR confirmation screen ────────────────────────────────────────────────────

function openQR(order) {
  document.getElementById("qr-order-type").textContent    = order.order_type === "pre_order" ? "PRE-ORDER" : "WALK-IN";
  document.getElementById("qr-timeslot").textContent      = order.timeslot.label;
  document.getElementById("qr-token-display").textContent = order.qr_token || "—";
  document.getElementById("qr-item").textContent          = order.item_name;
  document.getElementById("qr-slot").textContent          = order.timeslot.label;
  document.getElementById("qr-order-id").textContent      = `#ORD-${String(order.id).padStart(4,"0")}`;
  document.getElementById("qr-amount").textContent        = fmt(order.amount_charged);
  document.getElementById("qr-total").textContent         = fmt(order.amount_charged);

  const cancelBtn = document.getElementById("qr-cancel-btn");
  const isCancellable = ["pending","ready"].includes(order.status);
  cancelBtn.style.display = isCancellable ? "" : "none";
  cancelBtn.onclick = () => cancelOrder(order.id);

  if (order.qr_expires_at) {
    const exp = new Date(order.qr_expires_at);
    document.getElementById("qr-expires").textContent = `⏱ Hết hạn lúc ${exp.getHours().toString().padStart(2,"0")}:${exp.getMinutes().toString().padStart(2,"0")}`;
  } else {
    document.getElementById("qr-expires").textContent = "";
  }

  show("screen-qr");
}

async function cancelOrder(orderId) {
  if (!confirm("Hủy đơn này và hoàn tiền?")) return;
  try {
    await api("DELETE", `/orders/${orderId}`);
    state.account = await api("GET", `/students/${state.student.id}/account`);
    state.orders  = await api("GET", `/orders?student_id=${state.student.id}`);
    await loadDashboard();
  } catch (err) {
    showError(err.message || "Không thể hủy đơn.");
  }
}

// ── History screen ────────────────────────────────────────────────────────────

async function loadHistory() {
  show("screen-history");
  setNav("account");
  const [account, orders, txns] = await Promise.all([
    api("GET", `/students/${state.student.id}/account`),
    api("GET", `/orders?student_id=${state.student.id}`),
    api("GET", `/students/${state.student.id}/account/transactions`),
  ]);
  state.account = account;
  state.orders  = orders;

  document.getElementById("history-balance").textContent = fmt(account.balance);

  document.getElementById("txn-list").innerHTML = txns.map(t => {
    const isCredit = t.transaction_type === "top_up" || t.transaction_type === "refund";
    const icon = isCredit ? "💰" : "🍜";
    const label = { top_up: "Nạp tiền", deduction: "Đặt món", refund: "Hoàn tiền" }[t.transaction_type] || t.transaction_type;
    const d = new Date(t.created_at);
    const time = `${d.getHours().toString().padStart(2,"0")}:${d.getMinutes().toString().padStart(2,"0")}`;
    return `
      <div class="txn-item">
        <div class="txn-icon-wrap ${isCredit ? "credit" : "debit"}">${icon}</div>
        <div class="txn-info">
          <div class="txn-name">${label}${t.note ? ` · ${t.note}` : ""}</div>
          <div class="txn-date">${time} · Số dư sau: ${fmt(t.balance_after)}</div>
        </div>
        <div class="txn-amount ${isCredit ? "credit" : "debit"}">
          ${isCredit ? "+" : ""}${fmt(t.amount)}
        </div>
      </div>`;
  }).join("") || `<div style="padding:20px;text-align:center;color:var(--gray-400);">Chưa có giao dịch</div>`;
}

// ── Utilities ─────────────────────────────────────────────────────────────────

function fmt(vnd) {
  return `${Number(vnd).toLocaleString("vi-VN")} ₫`;
}

function statusBadge(status) {
  const map = {
    pending:   ["badge-warning", "Chờ lấy"],
    ready:     ["badge-success", "Sẵn sàng"],
    delivered: ["badge-gray",   "Đã giao"],
    cancelled: ["badge-danger", "Đã hủy"],
  };
  const [cls, label] = map[status] || ["badge-gray", status];
  return `<span class="badge ${cls}">${label}</span>`;
}

function itemEmoji(name) {
  if (name.includes("Bún") || name.includes("Phở") || name.includes("Mì")) return "🍜";
  if (name.includes("Cơm")) return "🍚";
  return "🍽️";
}

// ── Boot ──────────────────────────────────────────────────────────────────────

document.addEventListener("DOMContentLoaded", () => {
  // Login form
  document.getElementById("login-form").addEventListener("submit", async e => {
    e.preventDefault();
    const btn = document.getElementById("login-btn");
    btn.disabled = true;
    try {
      await login(
        document.getElementById("login-username").value,
        document.getElementById("login-password").value,
      );
    } catch (err) {
      showError(err.message || "Đăng nhập thất bại.");
      btn.disabled = false;
    }
  });

  // Bottom nav
  document.querySelectorAll(".nav-item").forEach(item => {
    item.addEventListener("click", () => {
      const tab = item.dataset.tab;
      if (tab === "home")    loadDashboard();
      if (tab === "menu")    loadMenu();
      if (tab === "orders")  { loadHistory(); }
      if (tab === "account") loadHistory();
    });
  });

  // Pre-order confirm button
  document.getElementById("po-confirm-btn").addEventListener("click", submitPreOrder);

  // QR back button
  document.getElementById("qr-back-btn").addEventListener("click", loadDashboard);

  // Check if already logged in
  if (state.sessionToken) {
    loadStudent("20110001").then(loadDashboard).catch(() => show("screen-login"));
  } else {
    show("screen-login");
  }
});
