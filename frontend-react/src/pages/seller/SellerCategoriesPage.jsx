import { useEffect, useState } from "react";
import { createSellerCategory, deleteSellerCategory, getSellerCategories, getSellerCategoryDetails, updateSellerCategory } from "../../services/apiService";

export default function SellerCategoriesPage() {
  const [items, setItems] = useState([]);
  const [name, setName] = useState("");
  const [msg, setMsg] = useState("");
  const [editingId, setEditingId] = useState(null);
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    setLoading(true);
    const data = await getSellerCategories();
    setItems(data || []);
    setLoading(false);
  };
  useEffect(() => {
    loadData();
  }, []);

  const save = async () => {
    try {
      const payload = { name: name.trim(), description: "" };
      if (!payload.name) {
        setMsg("Tên danh mục bắt buộc");
        return;
      }
      if (editingId) {
        await updateSellerCategory(editingId, payload);
        setMsg("Đã cập nhật danh mục");
      } else {
        await createSellerCategory(payload);
        setMsg("Đã tạo danh mục");
      }
      setName("");
      setEditingId(null);
      loadData();
    } catch (error) {
      setMsg(error?.response?.data?.message || "Không lưu được danh mục");
    }
  };

  const remove = async (id) => {
    try {
      await deleteSellerCategory(id);
      setMsg("Đã xóa danh mục");
      if (editingId === id) {
        setEditingId(null);
        setName("");
      }
      loadData();
    } catch (error) {
      setMsg(error?.response?.data?.message || "Không xóa được danh mục");
    }
  };

  const edit = async (id) => {
    try {
      const data = await getSellerCategoryDetails(id);
      setEditingId(id);
      setName(data.name || "");
      setMsg("Đang sửa danh mục - bấm Cập nhật để lưu");
    } catch (error) {
      setMsg(error?.response?.data?.message || "Không lấy được dữ liệu danh mục");
    }
  };

  return (
    <section className="page">
      <div className="row" style={{ justifyContent: "space-between" }}>
        <div>
          <p className="eyebrow">Danh mục seller</p>
          <h2>Quản lý danh mục món</h2>
        </div>
        <div className="row">
          <input placeholder="Tên danh mục" value={name} onChange={(e) => setName(e.target.value)} />
          <button type="button" onClick={save}>{editingId ? "Cập nhật" : "Thêm"}</button>
        </div>
      </div>
      {msg && <p className="ok">{msg}</p>}
      {loading ? (
        <div className="panel">Đang tải danh mục...</div>
      ) : items.length === 0 ? (
        <div className="panel soft-panel">Chưa có danh mục nào.</div>
      ) : (
        <table className="table">
          <thead><tr><th>ID</th><th>Tên</th><th>Số món</th><th /></tr></thead>
          <tbody>
            {items.map((x) => (
              <tr key={x.id}>
                <td>{x.id}</td>
                <td>{x.name}</td>
                <td>{x.foodCount}</td>
                <td>
                  <div className="action-bar">
                    <button className="secondary" onClick={() => edit(x.id)}>Sửa</button>
                    <button className="ghost" onClick={() => remove(x.id)}>Xóa</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
