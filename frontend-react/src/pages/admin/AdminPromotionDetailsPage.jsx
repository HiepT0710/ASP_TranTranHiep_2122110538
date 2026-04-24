import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { deleteAdminPromotion, deleteAdminVoucher, editAdminPromotion, editAdminVoucher, getAdminPromotionDetails, toggleAdminPromotion, toggleAdminVoucher } from "../../services/apiService";

const makePromotionForm = (p) => ({
  name: p?.name || "",
  description: p?.description || "",
  scope: p?.scope || "Food",
  discountPercent: p?.discountPercent ?? 10,
  startAt: p?.startAt ? String(p.startAt).slice(0, 16) : "",
  endAt: p?.endAt ? String(p.endAt).slice(0, 16) : "",
});

const makeVoucherForm = (v) => ({
  code: v?.code || "",
  note: v?.note || "",
  usageLimit: v?.usageLimit ?? 1,
  minOrderAmount: v?.minOrderAmount ?? "",
  maxDiscountAmount: v?.maxDiscountAmount ?? "",
  startAt: v?.startAt ? String(v.startAt).slice(0, 16) : "",
  endAt: v?.endAt ? String(v.endAt).slice(0, 16) : "",
});

export default function AdminPromotionDetailsPage() {
  const { id } = useParams();
  const [promotion, setPromotion] = useState(null);
  const [promoForm, setPromoForm] = useState(null);
  const [voucherForms, setVoucherForms] = useState({});
  const [msg, setMsg] = useState("");

  const load = async () => {
    const p = await getAdminPromotionDetails(id);
    setPromotion(p);
    setPromoForm(makePromotionForm(p));
    const formMap = {};
    for (const v of p.vouchers || []) formMap[v.id] = makeVoucherForm(v);
    setVoucherForms(formMap);
  };

  useEffect(() => { load(); }, [id]);

  if (!promotion || !promoForm) return <section className="page">Đang tải...</section>;

  const savePromotion = async (e) => {
    e.preventDefault();
    await editAdminPromotion(promotion.id, {
      name: promoForm.name,
      description: promoForm.description,
      scope: promoForm.scope,
      discountPercent: Number(promoForm.discountPercent),
      startAt: promoForm.startAt || null,
      endAt: promoForm.endAt || null,
    });
    setMsg("Đã cập nhật promotion");
    await load();
  };

  const saveVoucher = async (voucherId) => {
    const form = voucherForms[voucherId];
    await editAdminVoucher(voucherId, {
      promotionId: promotion.id,
      code: form.code,
      note: form.note,
      usageLimit: Number(form.usageLimit),
      minOrderAmount: form.minOrderAmount || null,
      maxDiscountAmount: form.maxDiscountAmount || null,
      startAt: form.startAt || null,
      endAt: form.endAt || null,
    });
    setMsg("Đã cập nhật voucher");
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
        <div className="row">
          <span className="badge">{promotion.scope}</span>
          <span className="badge">{promotion.discountPercent}%</span>
        </div>
        <div className="split">
          <input type="number" min="1" max="100" value={promoForm.discountPercent} onChange={(e) => setPromoForm({ ...promoForm, discountPercent: e.target.value })} />
          <input type="datetime-local" value={promoForm.startAt} onChange={(e) => setPromoForm({ ...promoForm, startAt: e.target.value })} />
        </div>
        <input type="datetime-local" value={promoForm.endAt} onChange={(e) => setPromoForm({ ...promoForm, endAt: e.target.value })} />
        <div className="row">
          <button type="submit">Lưu promotion</button>
          <button type="button" onClick={async () => { await toggleAdminPromotion(promotion.id); await load(); }}>Bật/tắt</button>
          <button type="button" className="secondary" onClick={async () => { await deleteAdminPromotion(promotion.id); window.location.assign('/admin/promotions'); }}>Xóa</button>
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
              <div className="split">
                <input value={form.usageLimit} onChange={(e) => setVoucherForms({ ...voucherForms, [v.id]: { ...form, usageLimit: e.target.value } })} />
                <input type="datetime-local" value={form.startAt} onChange={(e) => setVoucherForms({ ...voucherForms, [v.id]: { ...form, startAt: e.target.value } })} />
              </div>
              <div className="split">
                <input value={form.minOrderAmount} onChange={(e) => setVoucherForms({ ...voucherForms, [v.id]: { ...form, minOrderAmount: e.target.value } })} placeholder="Đơn tối thiểu" />
                <input value={form.maxDiscountAmount} onChange={(e) => setVoucherForms({ ...voucherForms, [v.id]: { ...form, maxDiscountAmount: e.target.value } })} placeholder="Giảm tối đa" />
              </div>
              <input type="datetime-local" value={form.endAt} onChange={(e) => setVoucherForms({ ...voucherForms, [v.id]: { ...form, endAt: e.target.value } })} />
              <div className="row">
                <button type="button" onClick={() => saveVoucher(v.id)}>Lưu voucher</button>
                <button type="button" onClick={async () => { await toggleAdminVoucher(v.id); await load(); }}>Bật/tắt</button>
                <button type="button" className="secondary" onClick={async () => { await deleteAdminVoucher(v.id); await load(); }}>Xóa</button>
              </div>
            </article>
          );
        })}
      </div>
    </section>
  );
}
