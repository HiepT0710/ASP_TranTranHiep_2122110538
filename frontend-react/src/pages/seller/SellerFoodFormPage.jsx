import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { createSellerFood, editSellerFood, getSellerCategories, getSellerFoodDetails, resolveImageUrl } from "../../services/apiService";

const initial = {
  name: "",
  price: "",
  description: "",
  categoryId: "",
  isAvailable: true,
  stockQuantity: "",
  imageFile: null,
  imagePreview: "",
};

export default function SellerFoodFormPage() {
  const { id } = useParams();
  const isEdit = !!id;
  const navigate = useNavigate();
  const [categories, setCategories] = useState([]);
  const [form, setForm] = useState(initial);
  const [msg, setMsg] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    const load = async () => {
      setLoading(true);
      setMsg("");
      try {
        const catsRes = await getSellerCategories();
        if (!mounted) return;
        const cats = Array.isArray(catsRes) ? catsRes : (catsRes.items || []);
        setCategories(cats);

        if (isEdit) {
          const foodRes = await getSellerFoodDetails(id);
          if (!mounted) return;
          setForm({
            ...initial,
            name: foodRes.name || "",
            price: foodRes.price?.toString?.() || "",
            description: foodRes.description || "",
            categoryId: foodRes.categoryId?.toString?.() || "",
            isAvailable: !!foodRes.isAvailable,
            stockQuantity: foodRes.stockQuantity?.toString?.() || "",
            imagePreview: resolveImageUrl(foodRes.image),
          });
        }
      } catch (error) {
        if (mounted) setMsg(error?.response?.data?.message || "Không tải được dữ liệu món");
      } finally {
        if (mounted) setLoading(false);
      }
    };
    load();
    return () => { mounted = false; };
  }, [id, isEdit]);

  const previewSrc = useMemo(() => {
    if (form.imageFile) return URL.createObjectURL(form.imageFile);
    return form.imagePreview;
  }, [form.imageFile, form.imagePreview]);

  const updateField = (key, value) => setForm((prev) => ({ ...prev, [key]: value }));

  const submit = async (e) => {
    e.preventDefault();
    const payload = {
      name: form.name,
      price: form.price,
      description: form.description,
      categoryId: form.categoryId,
      isAvailable: form.isAvailable,
      stockQuantity: form.stockQuantity,
      imageFile: form.imageFile || undefined,
    };
    try {
      if (isEdit) await editSellerFood(id, payload);
      else await createSellerFood(payload);
      navigate("/seller/foods");
    } catch (error) {
      setMsg(error?.response?.data?.message || "Lưu món thất bại");
    }
  };

  if (loading) {
    return (
      <section className="page">
        <p>Đang tải dữ liệu món...</p>
      </section>
    );
  }

  return (
    <section className="page">
      <div className="split">
        <div>
          <p className="eyebrow">Seller món ăn</p>
          <h2>{isEdit ? "Chỉnh sửa món" : "Thêm món mới"}</h2>
          <p className="muted">Nhập thông tin món ăn, xem trước ảnh và điều chỉnh giá/tồn kho theo kiểu nhập mượt hơn.</p>
          {msg && <p className="error">{msg}</p>}
          <form className="form" onSubmit={submit}>
            <input placeholder="Tên món" value={form.name} onChange={(e) => updateField("name", e.target.value)} />
            <div className="split">
              <input inputMode="decimal" placeholder="Giá bán" value={form.price} onChange={(e) => updateField("price", e.target.value)} />
              <input inputMode="numeric" placeholder="Số tồn kho" value={form.stockQuantity} onChange={(e) => updateField("stockQuantity", e.target.value)} />
            </div>
            <textarea placeholder="Mô tả" value={form.description} onChange={(e) => updateField("description", e.target.value)} />
            <select value={form.categoryId} onChange={(e) => updateField("categoryId", e.target.value)}>
              <option value="">-- Chọn danh mục --</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
            <label className="panel" style={{ display: "flex", gap: 10, alignItems: "center" }}>
              <input type="checkbox" checked={form.isAvailable} onChange={(e) => updateField("isAvailable", e.target.checked)} />
              <span>Đang bán / hiển thị cho khách</span>
            </label>
            <div className="panel soft-panel">
              <label className="eyebrow">Ảnh món</label>
              <input type="file" accept="image/*" onChange={(e) => updateField("imageFile", e.target.files?.[0] || null)} />
              <p className="muted" style={{ marginBottom: 0 }}>Ảnh sẽ được upload và hiển thị ở danh sách/chi tiết nếu backend trả về đường dẫn ảnh.</p>
            </div>
            <button type="submit">{isEdit ? "Cập nhật món" : "Tạo món"}</button>
          </form>
        </div>
        <aside className="panel" style={{ alignSelf: "start" }}>
          <h3>Xem trước</h3>
          {previewSrc ? (
            <img
              src={previewSrc}
              alt="Preview"
              style={{ width: "100%", maxHeight: 260, objectFit: "cover", borderRadius: 18, border: "1px solid var(--border)" }}
            />
          ) : (
            <div className="soft-panel" style={{ minHeight: 240, borderRadius: 18, display: "grid", placeItems: "center" }}>
              <p className="muted">Chưa có ảnh xem trước</p>
            </div>
          )}
          <div style={{ marginTop: 14 }}>
            <p className="muted" style={{ marginBottom: 6 }}>Tên món</p>
            <b>{form.name || "--"}</b>
            <p className="muted" style={{ marginBottom: 6, marginTop: 14 }}>Giá bán</p>
            <b>{form.price || "--"}</b>
            <p className="muted" style={{ marginBottom: 6, marginTop: 14 }}>Tồn kho</p>
            <b>{form.stockQuantity || "--"}</b>
          </div>
        </aside>
      </div>
    </section>
  );
}
