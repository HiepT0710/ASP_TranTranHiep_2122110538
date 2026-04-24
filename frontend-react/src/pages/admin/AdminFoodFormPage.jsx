import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { createAdminFood, editAdminFood, getAdminCategories, getAdminFoodDetails, getAdminRestaurants, resolveImageUrl } from "../../services/apiService";

const initial = {
  name: "",
  price: "",
  description: "",
  restaurantId: "",
  categoryId: "",
  isAvailable: true,
  stockQuantity: "",
  imageFile: null,
  imagePreview: "",
};

export default function AdminFoodFormPage() {
  const { id } = useParams();
  const isEdit = !!id;
  const navigate = useNavigate();
  const [restaurants, setRestaurants] = useState([]);
  const [categories, setCategories] = useState([]);
  const [form, setForm] = useState(initial);
  const [msg, setMsg] = useState("");

  useEffect(() => {
    getAdminRestaurants({ page: 1, pageSize: 200 }).then((x) => setRestaurants(x.items || []));
    getAdminCategories().then((x) => setCategories(x || []));
    if (isEdit) {
      getAdminFoodDetails(id).then((x) =>
        setForm({
          ...initial,
          name: x.name || "",
          price: x.price?.toString?.() || "",
          description: x.description || "",
          restaurantId: x.restaurantId?.toString?.() || "",
          categoryId: x.categoryId?.toString?.() || "",
          isAvailable: !!x.isAvailable,
          stockQuantity: x.stockQuantity?.toString?.() || "",
          imagePreview: resolveImageUrl(x.image),
        })
      );
    }
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
      restaurantId: form.restaurantId,
      categoryId: form.categoryId,
      isAvailable: form.isAvailable,
      stockQuantity: form.stockQuantity,
      imageFile: form.imageFile || undefined,
    };
    try {
      if (isEdit) await editAdminFood(id, payload);
      else await createAdminFood(payload);
      navigate("/admin/foods");
    } catch (error) {
      setMsg(error?.response?.data?.message || "Lưu món thất bại");
    }
  };

  return (
    <section className="page">
      <div className="split">
        <div>
          <p className="eyebrow">Admin món ăn</p>
          <h2>{isEdit ? "Chỉnh sửa món" : "Thêm món mới"}</h2>
          <p className="muted">Quản lý món toàn hệ thống với ảnh xem trước, nhập giá/tồn kho linh hoạt và form rõ ràng hơn.</p>
          {msg && <p className="error">{msg}</p>}
          <form className="form" onSubmit={submit}>
            <input placeholder="Tên món" value={form.name} onChange={(e) => updateField("name", e.target.value)} />
            <div className="split">
              <input inputMode="decimal" placeholder="Giá bán" value={form.price} onChange={(e) => updateField("price", e.target.value)} />
              <input inputMode="numeric" placeholder="Số tồn kho" value={form.stockQuantity} onChange={(e) => updateField("stockQuantity", e.target.value)} />
            </div>
            <textarea placeholder="Mô tả" value={form.description} onChange={(e) => updateField("description", e.target.value)} />
            <select value={form.restaurantId} onChange={(e) => updateField("restaurantId", e.target.value)}>
              <option value="">-- Chọn quán --</option>
              {restaurants.map((r) => (
                <option key={r.id} value={r.id}>{r.name}</option>
              ))}
            </select>
            <select value={form.categoryId} onChange={(e) => updateField("categoryId", e.target.value)}>
              <option value="">-- Chọn danh mục --</option>
              {categories.filter((c) => !form.restaurantId || c.restaurantId === Number(form.restaurantId)).map((c) => (
                <option key={c.id} value={c.id}>{c.restaurantName} - {c.name}</option>
              ))}
            </select>
            <label className="panel" style={{ display: "flex", gap: 10, alignItems: "center" }}>
              <input type="checkbox" checked={form.isAvailable} onChange={(e) => updateField("isAvailable", e.target.checked)} />
              <span>Đang bán / hiển thị cho khách</span>
            </label>
            <div className="panel soft-panel">
              <label className="eyebrow">Ảnh món</label>
              <input type="file" accept="image/*" onChange={(e) => updateField("imageFile", e.target.files?.[0] || null)} />
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
        </aside>
      </div>
    </section>
  );
}
