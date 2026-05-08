import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import ActionMenu from "../../components/ActionMenu";
import SortFilterBar from "../../components/SortFilterBar";
import { deleteAdminFood, getAdminFoods } from "../../services/apiService";
import { useToast } from "../../context/ToastContext";

export default function AdminFoodsPage() {
  const { pushToast } = useToast();
  const [data, setData] = useState({ items: [], page: 1, total: 0, totalPages: 1, pageSize: 10 });
  const [filter, setFilter] = useState({ page: 1, pageSize: 10, restaurantId: "", categoryId: "", q: "", sortBy: "name_asc" });
  const [msg, setMsg] = useState("");
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    setLoading(true);
    const res = await getAdminFoods(filter);
    const total = res.total || 0;
    const totalPages = Math.max(1, Math.ceil(total / filter.pageSize));
    const page = Math.min(res.page || filter.page, totalPages);
    setData({ ...res, total, totalPages, page });
    setFilter((current) => (current.page > totalPages ? { ...current, page: totalPages } : current));
    setLoading(false);
  };
  useEffect(() => {
    loadData();
  }, [filter.page, filter.pageSize, filter.restaurantId, filter.categoryId, filter.q, filter.sortBy]);

  const remove = async (id) => {
    try {
      await deleteAdminFood(id);
      const message = "Đã xóa món";
      setMsg(message);
      pushToast(message, "success");
      loadData();
    } catch (error) {
      const message = error?.response?.data?.message || "Không xóa được món";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  return (
    <section className="page">
      <div className="row" style={{ justifyContent: "space-between", alignItems: "center", flexWrap: "wrap" }}>
        <div>
          <p className="eyebrow">Admin món ăn</p>
          <h2>Quản lý món toàn hệ thống</h2>
        </div>
        <div className="row">
          <select value={filter.pageSize} onChange={(e) => setFilter({ ...filter, pageSize: Number(e.target.value), page: 1 })}>
            <option value={10}>10</option>
            <option value={20}>20</option>
            <option value={50}>50</option>
          </select>
          <Link to="/admin/foods/new">+ Thêm món</Link>
        </div>
      </div>
      <SortFilterBar
        value={filter}
        onChange={setFilter}
        filters={[
          { key: "restaurantId", label: "Restaurant ID", type: "input", placeholder: "Nhập ID quán" },
          { key: "categoryId", label: "Category ID", type: "input", placeholder: "Nhập ID danh mục" },
          { key: "q", label: "Từ khóa", type: "input", placeholder: "Tìm theo tên món" },
        ]}
        sortOptions={[
          { value: "name_asc", label: "Tên A → Z" },
          { value: "name_desc", label: "Tên Z → A" },
          { value: "price_asc", label: "Giá tăng dần" },
          { value: "price_desc", label: "Giá giảm dần" },
          { value: "newest", label: "Mới nhất" },
          { value: "oldest", label: "Cũ nhất" },
        ]}
      />
      {msg && <p className="ok">{msg}</p>}
      {loading ? (
        <div className="panel">Đang tải món ăn...</div>
      ) : (data.items || []).length === 0 ? (
        <div className="panel soft-panel">Không có món nào phù hợp.</div>
      ) : (
        <table className="table">
          <thead><tr><th>ID</th><th>Tên</th><th>Quán</th><th>Danh mục</th><th>Giá</th><th>Tồn kho</th><th>Actions</th></tr></thead>
          <tbody>
            {(data.items || []).map((x) => (
              <tr key={x.id}>
                <td>{x.id}</td>
                <td>{x.name}</td>
                <td>{x.restaurantName}</td>
                <td>{x.categoryName}</td>
                <td>{x.price}</td>
                <td>{x.stockQuantity}</td>
                <td>
                  <ActionMenu
                    label="Thao tác"
                    items={[
                      { label: "Sửa", onClick: () => window.location.assign(`/admin/foods/${x.id}/edit`) },
                      { label: "Xóa", onClick: () => remove(x.id), variant: "ghost" },
                    ]}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {data.totalPages > 1 && (
        <div className="row">
          <button className="secondary" disabled={filter.page <= 1} onClick={() => setFilter({ ...filter, page: filter.page - 1 })}>Prev</button>
          <span className="badge">Trang {data.page || filter.page} / {data.totalPages}</span>
          <button className="secondary" disabled={filter.page >= data.totalPages} onClick={() => setFilter({ ...filter, page: filter.page + 1 })}>Next</button>
        </div>
      )}
    </section>
  );
}
