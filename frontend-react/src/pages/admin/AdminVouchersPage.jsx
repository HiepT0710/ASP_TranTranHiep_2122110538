import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useToast } from "../../context/ToastContext";
import {
  createAdminVoucher,
  deleteAdminVoucher,
  getAdminPromotions,
  getAdminVouchers,
  toggleAdminVoucher,
} from "../../services/apiService";

const voucherInitial = {
  promotionId: "",
  code: "",
  note: "",
  minOrderAmount: "",
  maxDiscountAmount: "",
  usageLimit: 1,
  perUserLimit: 1,
  startAt: "",
  endAt: "",
};

function autoCode(prefix = "SALE") {
  return `${prefix}-${Math.random().toString(36).slice(2, 8).toUpperCase()}`;
}

function money(value) {
  const n = Number(value || 0);
  return Number.isFinite(n) ? n.toLocaleString("vi-VN") : "0";
}

export default function AdminVouchersPage() {
  const { pushToast } = useToast();
  const [vouchers, setVouchers] = useState([]);
  const [promotions, setPromotions] = useState([]);
  const [voucher, setVoucher] = useState(voucherInitial);
  const [msg, setMsg] = useState("");

  const load = async () => {
    try {
      const [v, p] = await Promise.all([getAdminVouchers(), getAdminPromotions()]);
      setVouchers(v.items || []);
      setPromotions(p.items || []);
    } catch (error) {
      const message = error?.response?.data?.message || "Không tải được dữ liệu voucher";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  useEffect(() => {
    load().catch(() => {
      setMsg("Không tải được dữ liệu voucher");
      pushToast("Không tải được dữ liệu voucher", "error");
    });
  }, []);

  const selectedPromotion = useMemo(
    () => promotions.find((p) => String(p.id) === String(voucher.promotionId)),
    [promotions, voucher.promotionId]
  );

  const submitVoucher = async (e) => {
    e.preventDefault();
    try {
      await createAdminVoucher({
        ...voucher,
        promotionId: Number(voucher.promotionId),
        usageLimit: Number(voucher.usageLimit),
        perUserLimit: Number(voucher.perUserLimit),
        minOrderAmount: voucher.minOrderAmount || null,
        maxDiscountAmount: voucher.maxDiscountAmount || null,
        startAt: voucher.startAt || null,
        endAt: voucher.endAt || null,
      });
      setVoucher(voucherInitial);
      setMsg("Đã tạo voucher");
      pushToast("Đã tạo voucher", "success");
      await load();
    } catch (error) {
      const message = error?.response?.data?.message || "Không tạo được voucher";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  return (
    <section className="page">
      <div className="page-header">
        <div>
          <h2>Voucher</h2>
          <p className="muted">Quản lý mã giảm giá, giới hạn lượt dùng và giới hạn theo từng user.</p>
        </div>
        <div className="row">
          <Link to="/admin/promotions" className="secondary">Xem promotion</Link>
        </div>
      </div>

      {msg && <p className="ok">{msg}</p>}

      <div className="split">
        <form className="panel form" onSubmit={submitVoucher}>
          <h3>Tạo voucher</h3>
          <select value={voucher.promotionId} onChange={(e) => setVoucher({ ...voucher, promotionId: e.target.value })}>
            <option value="">-- Chọn promotion --</option>
            {promotions.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name} · {p.scope} · {p.discountPercent}%
              </option>
            ))}
          </select>
          <div className="row">
            <input
              placeholder="CODE"
              value={voucher.code}
              onChange={(e) => setVoucher({ ...voucher, code: e.target.value.toUpperCase() })}
            />
            <button type="button" className="secondary" onClick={() => setVoucher({ ...voucher, code: autoCode() })}>
              Sinh code
            </button>
          </div>
          <input placeholder="Ghi chú" value={voucher.note} onChange={(e) => setVoucher({ ...voucher, note: e.target.value })} />
          <div className="split">
            <input placeholder="Đơn tối thiểu" value={voucher.minOrderAmount} onChange={(e) => setVoucher({ ...voucher, minOrderAmount: e.target.value })} />
            <input placeholder="Giảm tối đa" value={voucher.maxDiscountAmount} onChange={(e) => setVoucher({ ...voucher, maxDiscountAmount: e.target.value })} />
          </div>
          <div className="split">
            <input type="number" min="1" value={voucher.usageLimit} onChange={(e) => setVoucher({ ...voucher, usageLimit: e.target.value })} />
            <input type="number" min="1" max="100" value={voucher.perUserLimit} onChange={(e) => setVoucher({ ...voucher, perUserLimit: e.target.value })} />
          </div>
          <div className="split">
            <input type="datetime-local" value={voucher.startAt} onChange={(e) => setVoucher({ ...voucher, startAt: e.target.value })} />
            <input type="datetime-local" value={voucher.endAt} onChange={(e) => setVoucher({ ...voucher, endAt: e.target.value })} />
          </div>
          {selectedPromotion && (
            <div className="panel soft-panel" style={{ marginBottom: 0 }}>
              <p className="muted" style={{ marginBottom: 6 }}>Promotion đã chọn</p>
              <b>{selectedPromotion.name}</b>
              <p className="muted" style={{ marginBottom: 0 }}>
                Scope: {selectedPromotion.scope} · Giảm {selectedPromotion.discountPercent}%
              </p>
            </div>
          )}
          <button type="submit">Tạo voucher</button>
        </form>

        <div className="panel soft-panel">
          <h3>Tổng quan nhanh</h3>
          <div className="stat-grid">
            <div className="stat-card">
              <span>#</span>
              <strong>{vouchers.length}</strong>
              <p className="muted">Voucher</p>
            </div>
            <div className="stat-card">
              <span>✓</span>
              <strong>{vouchers.filter((v) => v.isActive).length}</strong>
              <p className="muted">Đang bật</p>
            </div>
            <div className="stat-card">
              <span>1</span>
              <strong>{vouchers.reduce((sum, v) => sum + (v.perUserLimit || 1), 0)}</strong>
              <p className="muted">Tổng giới hạn user</p>
            </div>
          </div>
        </div>
      </div>

      <h3>Danh sách voucher</h3>
      <div className="cards">
        {vouchers.map((v) => (
          <article
            key={v.id}
            className="panel"
            style={{
              borderLeft: v.isActive ? "4px solid #16a34a" : "4px solid #94a3b8",
              background: v.isActive ? "linear-gradient(180deg, rgba(240,253,244,.95), rgba(255,255,255,.98))" : "#fff",
            }}
          >
            <div className="row" style={{ justifyContent: "space-between" }}>
              <div>
                <b style={{ fontSize: 18 }}>{v.code}</b>
                <p className="muted" style={{ marginBottom: 0 }}>{v.promotionName}</p>
              </div>
              <span className="badge" style={{ background: v.isActive ? "#dcfce7" : "#e2e8f0", color: v.isActive ? "#166534" : "#334155" }}>
                {v.isActive ? "Đang bật" : "Đang tắt"}
              </span>
            </div>

            <div className="stat-grid" style={{ marginTop: 12 }}>
              <div className="stat-card">
                <span>↗</span>
                <strong>{v.usedCount}/{v.usageLimit}</strong>
                <p className="muted">Lượt dùng</p>
              </div>
              <div className="stat-card">
                <span>👤</span>
                <strong>{v.perUserLimit || 1}</strong>
                <p className="muted">Mỗi user</p>
              </div>
              <div className="stat-card">
                <span>₫</span>
                <strong>{money(v.minOrderAmount || 0)}</strong>
                <p className="muted">Đơn tối thiểu</p>
              </div>
            </div>

            <div className="row" style={{ marginTop: 12, flexWrap: "wrap" }}>
              <span className="badge">Giảm {v.discountPercent}%</span>
              {v.maxDiscountAmount != null && <span className="badge">Tối đa {money(v.maxDiscountAmount)}</span>}
              <span className="badge">{v.promotionScope}</span>
            </div>

            <p className="muted" style={{ marginTop: 12, marginBottom: 8 }}>
              Thời gian: {v.startAt ? new Date(v.startAt).toLocaleString("vi-VN") : "--"} → {v.endAt ? new Date(v.endAt).toLocaleString("vi-VN") : "--"}
            </p>
            <p>{v.note || "Voucher áp dụng theo điều kiện của promotion."}</p>

            <div className="row">
              <button
                onClick={async () => {
                  await toggleAdminVoucher(v.id);
                  pushToast("Đã đổi trạng thái voucher", "info");
                  await load();
                }}
              >
                Bật/tắt
              </button>
              <button
                className="secondary"
                onClick={async () => {
                  await deleteAdminVoucher(v.id);
                  pushToast("Đã xóa voucher", "success");
                  await load();
                }}
              >
                Xóa
              </button>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
