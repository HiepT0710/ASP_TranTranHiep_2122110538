import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useToast } from "../../context/ToastContext";
import { deleteAdminPromotion, deleteAdminVoucher, editAdminPromotion, editAdminVoucher, getAdminFoods, getAdminPromotionDetails, getAdminPromotions, toggleAdminPromotion, toggleAdminVoucher } from "../../services/apiService";

const toLocalDateTime = (value) => {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return String(value).slice(0, 16);
  const tzOffset = date.getTimezoneOffset() * 60000;
  return new Date(date.getTime() - tzOffset).toISOString().slice(0, 16);
};

const fromLocalDateTime = (value) => (value ? new Date(value).toISOString() : null);

const makePromotionForm = (p) => ({
  name: p?.name || "",
  description: p?.description || "",
  scope: p?.scope || "Food",
  discountPercent: p?.discountPercent ?? 10,
  restaurantId: p?.restaurantId ?? "",
  foodId: p?.foodId ?? "",
  startAt: toLocalDateTime(p?.startAt),
  endAt: toLocalDateTime(p?.endAt),
});

const makeVoucherForm = (v) => ({
  promotionId: v?.promotionId ?? "",
  code: v?.code || "",
  note: v?.note || "",
  usageLimit: v?.usageLimit ?? 1,
  perUserLimit: v?.perUserLimit ?? 1,
  minOrderAmount: v?.minOrderAmount ?? "",
  maxDiscountAmount: v?.maxDiscountAmount ?? "",
  startAt: toLocalDateTime(v?.startAt),
  endAt: toLocalDateTime(v?.endAt),
});

export default function AdminPromotionDetailsPage() {
  const { pushToast } = useToast();
  const { id } = useParams();
  const [promotion, setPromotion] = useState(null);
  const [promoForm, setPromoForm] = useState(null);
  const [voucherForms, setVoucherForms] = useState({});
  const [promotions, setPromotions] = useState([]);
  const [foods, setFoods] = useState([]);
  const [msg, setMsg] = useState("");

  const load = async () => {
    const [p, list, foodList] = await Promise.all([getAdminPromotionDetails(id), getAdminPromotions(), getAdminFoods({ pageSize: 200 })]);
    setPromotion(p);
    setPromotions(list.items || []);
    setFoods(foodList.items || []);
    setPromoForm(makePromotionForm(p));
    const formMap = {};
    for (const v of p.vouchers || []) formMap[v.id] = makeVoucherForm(v);
    setVoucherForms(formMap);
  };

  useEffect(() => { load().catch(() => setMsg("Không tải được dữ liệu promotion")); }, [id]);

  if (!promotion || !promoForm) return <section className="page">Đang tải...</section>;

  const savePromotion = async (e) => {
    e.preventDefault();
    await editAdminPromotion(promotion.id, {
      name: promoForm.name,
      description: promoForm.description,
      scope: promoForm.scope,
      restaurantId: promoForm.scope === "Restaurant" ? (promoForm.restaurantId ? Number(promoForm.restaurantId) : null) : null,
      foodId: promoForm.scope === "Food" ? (promoForm.foodId ? Number(promoForm.foodId) : null) : null,
      discountPercent: Number(promoForm.discountPercent),
      startAt: fromLocalDateTime(promoForm.startAt),
      endAt: fromLocalDateTime(promoForm.endAt),
    });
    setMsg("Đã cập nhật promotion");
    pushToast("Đã cập nhật promotion", "success");
    await load();
  };

  const saveVoucher = async (voucherId) => {
    const form = voucherForms[voucherId];
    await editAdminVoucher(voucherId, {
      promotionId: form.promotionId ? Number(form.promotionId) : promotion.id,
      code: form.code,
      note: form.note,
      usageLimit: Number(form.usageLimit),
      perUserLimit: Number(form.perUserLimit),
      minOrderAmount: form.minOrderAmount === "" ? null : Number(form.minOrderAmount),
      maxDiscountAmount: form.maxDiscountAmount === "" ? null : Number(form.maxDiscountAmount),
      startAt: fromLocalDateTime(form.startAt),
      endAt: fromLocalDateTime(form.endAt),
    });
    setMsg("Đã cập nhật voucher");
    pushToast("Đã cập nhật voucher", "success");
    await load();
  };

  return (
    <section className="page">
      <div className="page-header">
        <div>
          <h2>Chi tiết promotion</h2>
          <p className="muted">Sửa trực tiếp tại trang này.</p>
        </div>
        <Link to="/admin/promotions" className="secondary">Quay lại</Link>
      </div>

      {msg && <p className="ok">{msg}</p>}

      <form className="panel form" onSubmit={savePromotion}>
        <h3>Chỉnh sửa promotion</h3>
        <input value={promoForm.name} onChange={(e) => setPromoForm({ ...promoForm, name: e.target.value })} />
        <textarea value={promoForm.description} onChange={(e) => setPromoForm({ ...promoForm, description: e.target.value })} />
        <div className="split">
          <select value={promoForm.scope} onChange={(e) => setPromoForm({ ...promoForm, scope: e.target.value, restaurantId: e.target.value === "Restaurant" ? promoForm.restaurantId : "", foodId: e.target.value === "Food" ? promoForm.foodId : "" })}>
            <option value="Food">Khuyến mãi món</option>
            <option value="Restaurant">Khuyến mãi quán</option>
          </select>
          <input type="number" min="1" max="100" value={promoForm.discountPercent} onChange={(e) => setPromoForm({ ...promoForm, discountPercent: e.target.value })} />
        </div>
        {promoForm.scope === "Restaurant" ? (
          <input
            placeholder="RestaurantId"
            value={promoForm.restaurantId}
            onChange={(e) => setPromoForm({ ...promoForm, restaurantId: e.target.value })}
          />
        ) : (
          <select value={promoForm.foodId} onChange={(e) => setPromoForm({ ...promoForm, foodId: e.target.value })}>
            <option value="">-- Chọn món --</option>
            {foods.map((food) => (
              <option key={food.id} value={food.id}>
                {food.name}
              </option>
            ))}
          </select>
        )}
        <div className="row">
          <span className="badge">{promotion.scope}</span>
          <span className="badge">{promotion.discountPercent}%</span>
        </div>
        <div className="split">
          <input type="datetime-local" value={promoForm.startAt} onChange={(e) => setPromoForm({ ...promoForm, startAt: e.target.value })} />
          <input type="datetime-local" value={promoForm.endAt} onChange={(e) => setPromoForm({ ...promoForm, endAt: e.target.value })} />
        </div>
        <div className="row">
          <button type="submit">Lưu promotion</button>
          <button type="button" onClick={async () => { await toggleAdminPromotion(promotion.id); pushToast("Đã đổi trạng thái promotion", "info"); await load(); }}>Bật/tắt</button>
          <button type="button" className="secondary" onClick={async () => { await deleteAdminPromotion(promotion.id); pushToast("Đã xóa promotion", "success"); window.location.assign('/admin/promotions'); }}>Xóa</button>
        </div>
      </form>

      <h3>Voucher</h3>
      <div className="cards">
        {(promotion.vouchers || []).map((v) => {
          const form = voucherForms[v.id];
          if (!form) return null;
          return (
            <article key={v.id} className="panel form">
              <div className="row" style={{ justifyContent: "space-between" }}>
                <b>{v.code}</b>
                <span className="badge">{v.isActive ? "Đang bật" : "Đang tắt"}</span>
              </div>
              <input value={form.code} onChange={(e) => setVoucherForms({ ...voucherForms, [v.id]: { ...form, code: e.target.value } })} />
              <textarea value={form.note} onChange={(e) => setVoucherForms({ ...voucherForms, [v.id]: { ...form, note: e.target.value } })} />
              <select value={form.promotionId || promotion.id} onChange={(e) => setVoucherForms({ ...voucherForms, [v.id]: { ...form, promotionId: e.target.value } })}>
                <option value={promotion.id}>{promotion.name}</option>
                {promotions.filter((p) => p.id !== promotion.id).map((p) => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
              <div className="split">
                <input type="number" value={form.usageLimit} onChange={(e) => setVoucherForms({ ...voucherForms, [v.id]: { ...form, usageLimit: e.target.value } })} />
                <input type="number" min="1" max="100" value={form.perUserLimit} onChange={(e) => setVoucherForms({ ...voucherForms, [v.id]: { ...form, perUserLimit: e.target.value } })} placeholder="Mỗi user" />
              </div>
              <div className="split">
                <input type="number" value={form.minOrderAmount} onChange={(e) => setVoucherForms({ ...voucherForms, [v.id]: { ...form, minOrderAmount: e.target.value } })} placeholder="Đơn tối thiểu" />
                <input type="number" value={form.maxDiscountAmount} onChange={(e) => setVoucherForms({ ...voucherForms, [v.id]: { ...form, maxDiscountAmount: e.target.value } })} placeholder="Giảm tối đa" />
              </div>
              <input type="datetime-local" value={form.endAt} onChange={(e) => setVoucherForms({ ...voucherForms, [v.id]: { ...form, endAt: e.target.value } })} />
              <div className="row">
                <button type="button" onClick={() => saveVoucher(v.id)}>Lưu voucher</button>
                <button type="button" onClick={async () => { await toggleAdminVoucher(v.id); pushToast("Đã đổi trạng thái voucher", "info"); await load(); }}>Bật/tắt</button>
                <button type="button" className="secondary" onClick={async () => { await deleteAdminVoucher(v.id); pushToast("Đã xóa voucher", "success"); await load(); }}>Xóa</button>
              </div>
            </article>
          );
        })}
      </div>
    </section>
  );
}
