import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import ActionMenu from "../../components/ActionMenu";
import { deleteAdminFood, getAdminFoods } from "../../services/apiService";
import { useToast } from "../../context/ToastContext";

export default function AdminFoodsPage() {
  const { pushToast } = useToast();
  const [data, setData] = useState({ items: [], page: 1, total: 0, pageSize: 10 });
  const [filter, setFilter] = useState({ page: 1, pageSize: 10, restaurantId: "" });
  const [msg, setMsg] = useState("");
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    setLoading(true);
    const res = await getAdminFoods(filter);
    setData(res);
    setLoading(false);
  };
  useEffect(() => {
    loadData();
  }, [filter.page, filter.pageSize, filter.restaurantId]);

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
      <div className="row" style={{ justifyContent: "space-between" }}>
        <div>
          <p className="eyebrow">Admin món ăn</p>
          <h2>Quản lý món toàn hệ thống</h2>
        </div>
        <div className="row">
          <input placeholder="RestaurantId" value={filter.restaurantId} onChange={(e) => setFilter({ ...filter, restaurantId: e.target.value, page: 1 })} />
          <select value={filter.pageSize} onChange={(e) => setFilter({ ...filter, pageSize: Number(e.target.value), page: 1 })}>
            <option value={10}>10</option>
            <option value={20}>20</option>
            <option value={50}>50</option>
          </select>
          <Link to="/admin/foods/new">+ Thêm món</Link>
        </div>
      </div>
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
      <div className="row">
        <button disabled={filter.page <= 1} onClick={() => setFilter({ ...filter, page: filter.page - 1 })}>Prev</button>
        <span>Trang {data.page || filter.page}</span>
        <button onClick={() => setFilter({ ...filter, page: filter.page + 1 })}>Next</button>
      </div>
    </section>
  );
}
