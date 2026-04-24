import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useToast } from "../../context/ToastContext";
import { createSellerPromotion, createSellerVoucher, getSellerFoods, getSellerPromotions, getSellerVouchers, deleteSellerPromotion, deleteSellerVoucher, toggleSellerPromotion, toggleSellerVoucher } from "../../services/apiService";

const promoInitial = { name: "", description: "", scope: "Food", foodId: "", discountPercent: 10, startAt: "", endAt: "" };
const voucherInitial = { promotionId: "", code: "", note: "", minOrderAmount: "", maxDiscountAmount: "", usageLimit: 1, startAt: "", endAt: "" };

function autoCode(prefix = "SALE") {
  return `${prefix}-${Math.random().toString(36).slice(2, 8).toUpperCase()}`;
}

export default function SellerPromotionsPage() {
  const { pushToast } = useToast();
  const [promotions, setPromotions] = useState([]);
  const [vouchers, setVouchers] = useState([]);
  const [foods, setFoods] = useState([]);
  const [promo, setPromo] = useState(promoInitial);
  const [voucher, setVoucher] = useState(voucherInitial);
  const [msg, setMsg] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    setError("");
    try {
      const [p, v, f] = await Promise.all([getSellerPromotions(), getSellerVouchers(), getSellerFoods({ pageSize: 200 })]);
      setPromotions(p.items || []);
      setVouchers(v.items || []);
      setFoods(f.items || []);
    } catch (e) {
      setError(e?.response?.data?.message || "Không tải được khuyến mãi/voucher");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const promoOptions = useMemo(() => foods.map((f) => ({ id: f.id, label: `${f.name} (${f.categoryName || "Món"})` })), [foods]);

  const submitPromo = async (e) => {
    e.preventDefault();
    try {
      await createSellerPromotion({
        name: promo.name,
        description: promo.description,
        scope: promo.scope,
        foodId: promo.scope === "Food" && promo.foodId ? Number(promo.foodId) : null,
        discountPercent: Number(promo.discountPercent),
        startAt: promo.startAt || null,
        endAt: promo.endAt || null,
      });
      setPromo(promoInitial);
      setMsg("Đã tạo promotion");
      pushToast("Đã tạo promotion", "success");
      await load();
    } catch (e) {
      const message = e?.response?.data?.message || "Không tạo được promotion";
      setError(message);
      pushToast(message, "error");
    }
  };

  const submitVoucher = async (e) => {
    e.preventDefault();
    try {
      await createSellerVoucher({
        promotionId: Number(voucher.promotionId),
        code: voucher.code || autoCode(),
        note: voucher.note,
        usageLimit: Number(voucher.usageLimit),
        minOrderAmount: voucher.minOrderAmount || null,
        maxDiscountAmount: voucher.maxDiscountAmount || null,
        startAt: voucher.startAt || null,
        endAt: voucher.endAt || null,
      });
      setVoucher(voucherInitial);
      setMsg("Đã tạo voucher");
      pushToast("Đã tạo voucher", "success");
      await load();
    } catch (e) {
      const message = e?.response?.data?.message || "Không tạo được voucher";
      setError(message);
      pushToast(message, "error");
    }
  };

  if (loading) return <section className="page"><p>Đang tải khuyến mãi...</p></section>;

  return (
    <section className="page">
      <div className="page-header">
        <div>
          <h2>Khuyến mãi của quán</h2>
          <p className="muted">Tạo nhanh, rồi bấm vào từng promotion để xem / sửa / xóa ở trang riêng.</p>
        </div>
        <div className="row">
          <Link to="/seller/promotions" className="secondary">Trang quản lý</Link>
        </div>
      </div>

      {msg && <p className="ok">{msg}</p>}
      {error && <p className="error">{error}</p>}

      <div className="split">
        <form className="panel form" onSubmit={submitPromo}>
          <h3>Tạo promotion</h3>
          <input placeholder="Tên chương trình" value={promo.name} onChange={(e) => setPromo({ ...promo, name: e.target.value })} />
          <textarea placeholder="Mô tả" value={promo.description} onChange={(e) => setPromo({ ...promo, description: e.target.value })} />
          <select value={promo.scope} onChange={(e) => setPromo({ ...promo, scope: e.target.value })}>
            <option value="Food">Khuyến mãi món</option>
            <option value="Restaurant">Khuyến mãi quán</option>
          </select>
          {promo.scope === "Food" && (
            <select value={promo.foodId} onChange={(e) => setPromo({ ...promo, foodId: e.target.value })}>
              <option value="">-- Chọn món --</option>
              {promoOptions.map((f) => <option key={f.id} value={f.id}>{f.label}</option>)}
            </select>
          )}
          <div className="split">
            <input type="number" min="1" max="100" placeholder="% giảm" value={promo.discountPercent} onChange={(e) => setPromo({ ...promo, discountPercent: e.target.value })} />
            <input type="datetime-local" value={promo.startAt} onChange={(e) => setPromo({ ...promo, startAt: e.target.value })} />
          </div>
          <input type="datetime-local" value={promo.endAt} onChange={(e) => setPromo({ ...promo, endAt: e.target.value })} />
          <button type="submit">Tạo promotion</button>
        </form>

        <form className="panel form" onSubmit={submitVoucher}>
          <h3>Tạo voucher</h3>
          <select value={voucher.promotionId} onChange={(e) => setVoucher({ ...voucher, promotionId: e.target.value })}>
            <option value="">-- Chọn promotion --</option>
            {promotions.map((p) => <option key={p.id} value={p.id}>{p.name} ({p.discountPercent}%)</option>)}
          </select>
          <div className="row">
            <input placeholder="CODE" value={voucher.code} onChange={(e) => setVoucher({ ...voucher, code: e.target.value })} />
            <button type="button" className="secondary" onClick={() => setVoucher({ ...voucher, code: autoCode() })}>Sinh code</button>
          </div>
          <input placeholder="Ghi chú" value={voucher.note} onChange={(e) => setVoucher({ ...voucher, note: e.target.value })} />
          <div className="split">
            <input placeholder="Đơn tối thiểu" value={voucher.minOrderAmount} onChange={(e) => setVoucher({ ...voucher, minOrderAmount: e.target.value })} />
            <input placeholder="Giảm tối đa" value={voucher.maxDiscountAmount} onChange={(e) => setVoucher({ ...voucher, maxDiscountAmount: e.target.value })} />
          </div>
          <div className="split">
            <input type="number" min="1" value={voucher.usageLimit} onChange={(e) => setVoucher({ ...voucher, usageLimit: e.target.value })} />
            <input type="datetime-local" value={voucher.startAt} onChange={(e) => setVoucher({ ...voucher, startAt: e.target.value })} />
          </div>
          <input type="datetime-local" value={voucher.endAt} onChange={(e) => setVoucher({ ...voucher, endAt: e.target.value })} />
          <button type="submit">Tạo voucher</button>
        </form>
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
              <Link to={`/seller/promotions/${p.id}`} className="secondary">Xem / sửa</Link>
              <button onClick={async () => { await toggleSellerPromotion(p.id); pushToast("Đã đổi trạng thái promotion", "info"); await load(); }}>Bật/tắt</button>
              <button className="secondary" onClick={async () => { await deleteSellerPromotion(p.id); pushToast("Đã xóa promotion", "success"); await load(); }}>Xóa</button>
            </div>
          </article>
        ))}
      </div>

      <h3>Voucher</h3>
      <div className="cards">
        {vouchers.map((v) => (
          <article key={v.id} className="panel">
            <div className="row" style={{ justifyContent: "space-between" }}>
              <b>{v.code}</b>
              <span className="badge">{v.discountPercent}%</span>
            </div>
            <p className="muted">{v.promotionName}</p>
            <p>{v.isActive ? "Đang bật" : "Đang tắt"}</p>
            <div className="row">
              <button onClick={async () => { await toggleSellerVoucher(v.id); pushToast("Đã đổi trạng thái voucher", "info"); await load(); }}>Bật/tắt</button>
              <button className="secondary" onClick={async () => { await deleteSellerVoucher(v.id); pushToast("Đã xóa voucher", "success"); await load(); }}>Xóa</button>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
