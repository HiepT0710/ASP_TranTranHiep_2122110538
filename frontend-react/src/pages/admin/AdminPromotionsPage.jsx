import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useToast } from "../../context/ToastContext";
import { createAdminPromotion, deleteAdminPromotion, getAdminFoods, getAdminPromotions, toggleAdminPromotion } from "../../services/apiService";

const promoInitial = { name: "", description: "", scope: "Food", restaurantId: "", foodId: "", discountPercent: 10, startAt: "", endAt: "" };

function autoCode(prefix = "SALE") {
  return `${prefix}-${Math.random().toString(36).slice(2, 8).toUpperCase()}`;
}

export default function AdminPromotionsPage() {
  const { pushToast } = useToast();
  const [promotions, setPromotions] = useState([]);
  const [foods, setFoods] = useState([]);
  const [promo, setPromo] = useState(promoInitial);
  const [msg, setMsg] = useState("");

  const load = async () => {
    try {
      const [p, f] = await Promise.all([getAdminPromotions(), getAdminFoods({ pageSize: 200 })]);
      setPromotions(p.items || []);
      setFoods(f.items || []);
    } catch (error) {
      const message = error?.response?.data?.message || "Không tải được dữ liệu khuyến mãi";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  useEffect(() => { load().catch(() => { setMsg("Không tải được dữ liệu khuyến mãi"); pushToast("Không tải được dữ liệu khuyến mãi", "error"); }); }, []);

  const promoOptions = useMemo(() => foods.map((f) => ({ id: f.id, label: `${f.name} (${f.categoryName || "Món"})` })), [foods]);

  const submitPromo = async (e) => {
    e.preventDefault();
    try {
      await createAdminPromotion({
        ...promo,
        restaurantId: promo.restaurantId || null,
        foodId: promo.foodId || null,
        discountPercent: Number(promo.discountPercent),
        startAt: promo.startAt || null,
        endAt: promo.endAt || null,
      });
      setPromo(promoInitial);
      setMsg("Đã tạo promotion");
      pushToast("Đã tạo promotion", "success");
      await load();
    } catch (error) {
      const message = error?.response?.data?.message || "Không tạo được promotion";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  return (
    <section className="page">
      <div className="page-header">
        <div>
          <h2>Khuyến mãi</h2>
          <p className="muted">Quản lý promotion theo món hoặc quán, voucher đã được tách sang trang riêng.</p>
        </div>
        <div className="row">
          <Link to="/admin/vouchers" className="secondary">Quản lý voucher</Link>
        </div>
      </div>

      {msg && <p className="ok">{msg}</p>}

      <div className="split">
        <form className="panel form" onSubmit={submitPromo}>
          <h3>Tạo promotion</h3>
          <input placeholder="Tên chương trình" value={promo.name} onChange={(e) => setPromo({ ...promo, name: e.target.value })} />
          <textarea placeholder="Mô tả" value={promo.description} onChange={(e) => setPromo({ ...promo, description: e.target.value })} />
          <select value={promo.scope} onChange={(e) => setPromo({ ...promo, scope: e.target.value })}>
            <option value="Food">Khuyến mãi món</option>
            <option value="Restaurant">Khuyến mãi quán</option>
          </select>
          {promo.scope === "Restaurant" && (
            <input placeholder="RestaurantId" value={promo.restaurantId} onChange={(e) => setPromo({ ...promo, restaurantId: e.target.value })} />
          )}
          {promo.scope === "Food" && (
            <select value={promo.foodId} onChange={(e) => setPromo({ ...promo, foodId: e.target.value })}>
              <option value="">-- Chọn món --</option>
              {promoOptions.map((f) => <option key={f.id} value={f.id}>{f.label}</option>)}
            </select>
          )}
          <div className="split">
            <input type="number" min="1" max="100" placeholder="% giảm" value={promo.discountPercent} onChange={(e) => setPromo({ ...promo, discountPercent: e.target.value })} />
            <label className="form-field">
              <span>Ngày bắt đầu</span>
              <input type="datetime-local" value={promo.startAt} onChange={(e) => setPromo({ ...promo, startAt: e.target.value })} aria-label="Ngày bắt đầu" />
            </label>
          </div>
          <label className="form-field">
            <span>Ngày kết thúc</span>
            <input type="datetime-local" value={promo.endAt} onChange={(e) => setPromo({ ...promo, endAt: e.target.value })} aria-label="Ngày kết thúc" />
          </label>
          <button type="submit">Tạo promotion</button>
        </form>

        <div className="panel soft-panel">
          <h3>Gợi ý nhanh</h3>
          <p className="muted">Promotion chỉ quản lý % giảm theo quán hoặc món. Voucher là lớp mã riêng, dùng để thêm điều kiện như đơn tối thiểu hoặc giới hạn mỗi user.</p>
          <div className="stat-grid" style={{ marginTop: 16 }}>
            <div className="stat-card">
              <span>#</span>
              <strong>{promotions.length}</strong>
              <p className="muted">Promotion</p>
            </div>
            <div className="stat-card">
              <span>↗</span>
              <strong>{promotions.filter((p) => p.isActive).length}</strong>
              <p className="muted">Đang bật</p>
            </div>
            <div className="stat-card">
              <span>⏱</span>
              <strong>{promotions.filter((p) => new Date(p.endAt) > new Date()).length}</strong>
              <p className="muted">Còn hạn</p>
            </div>
          </div>
          <div className="row" style={{ marginTop: 16 }}>
            <Link to="/admin/vouchers" className="button">Đi tới voucher</Link>
            <Link to="/admin/promotions" className="button secondary">Ở lại trang promotion</Link>
          </div>
        </div>
      </div>

      <h3>Promotion</h3>
      <div className="cards">
        {promotions.map((p) => (
          <article key={p.id} className="panel">
            <div className="row" style={{ justifyContent: "space-between" }}>
              <div>
                <b>{p.name}</b>
                <p className="muted">{p.scope} - {p.discountPercent}%</p>
              </div>
              <span className="badge">{p.isActive ? "Đang bật" : "Đang tắt"}</span>
            </div>
            <p>{p.description}</p>
            <div className="row">
              <Link to={`/admin/promotions/${p.id}`} className="secondary">Xem / sửa</Link>
              <button onClick={async () => { await toggleAdminPromotion(p.id); pushToast("Đã đổi trạng thái promotion", "info"); await load(); }}>Bật/tắt</button>
              <button className="secondary" onClick={async () => { await deleteAdminPromotion(p.id); pushToast("Đã xóa promotion", "success"); await load(); }}>Xóa</button>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
