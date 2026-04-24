import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import ActionMenu from "../../components/ActionMenu";
import { adminUpdateOrder, getAdminOrders } from "../../services/apiService";
import { useToast } from "../../context/ToastContext";

export default function AdminOrdersPage() {
  const { pushToast } = useToast();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState("");
  const [filter, setFilter] = useState({ page: 1, pageSize: 15, status: "", restaurantId: "" });
  const [meta, setMeta] = useState({ page: 1, total: 0 });

  const loadData = async () => {
    setLoading(true);
    const data = await getAdminOrders(filter);
    setItems(data.items || []);
    setMeta({ page: data.page || filter.page, total: data.total || 0 });
    setLoading(false);
  };
  useEffect(() => {
    loadData();
  }, [filter.page, filter.pageSize, filter.status, filter.restaurantId]);

  const update = async (id, status) => {
    try {
      await adminUpdateOrder(id, status);
      const message = `Đã cập nhật trạng thái sang ${status}`;
      setMsg(message);
      pushToast(message, "success");
      loadData();
    } catch (error) {
      const message = error?.response?.data?.message || "Không cập nhật được trạng thái";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  return (
    <section className="page">
      <div className="row" style={{ justifyContent: "space-between" }}>
        <div>
          <p className="eyebrow">Quản trị đơn</p>
          <h2>Admin - Quản lý đơn hàng</h2>
        </div>
        <div className="row">
          <input placeholder="Restaurant ID" value={filter.restaurantId} onChange={(e) => setFilter({ ...filter, restaurantId: e.target.value, page: 1 })} />
          <select value={filter.status} onChange={(e) => setFilter({ ...filter, status: e.target.value, page: 1 })}>
            <option value="">Tất cả trạng thái</option>
            <option value="Pending">Pending</option>
            <option value="Preparing">Preparing</option>
            <option value="Delivering">Delivering</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
          <select value={filter.pageSize} onChange={(e) => setFilter({ ...filter, pageSize: Number(e.target.value), page: 1 })}>
            <option value={15}>15</option>
            <option value={30}>30</option>
            <option value={50}>50</option>
          </select>
        </div>
      </div>
      {msg && <p className="ok">{msg}</p>}
      {loading ? (
        <div className="panel">Đang tải đơn hàng...</div>
      ) : items.length === 0 ? (
        <div className="panel soft-panel">Không có đơn nào phù hợp bộ lọc.</div>
      ) : (
        <table className="table">
          <thead><tr><th>ID</th><th>Khách</th><th>Quán</th><th>Tổng tiền</th><th>Trạng thái</th><th>Cập nhật</th><th>Chi tiết</th></tr></thead>
          <tbody>
            {items.map((o) => (
              <tr key={o.id}>
                <td>{o.id}</td>
                <td>{o.username}</td>
                <td>{o.restaurantName}</td>
                <td>{o.totalAmount}</td>
                <td><span className="badge">{o.status}</span></td>
                <td>
                  <ActionMenu
                    label="Cập nhật"
                    items={[
                      { label: "Pending", onClick: () => update(o.id, "Pending") },
                      { label: "Preparing", onClick: () => update(o.id, "Preparing") },
                      { label: "Cancelled", onClick: () => update(o.id, "Cancelled"), variant: "ghost" },
                    ]}
                  />
                </td>
                <td><Link to={`/orders/${o.id}`}>Xem</Link></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      <div className="row">
        <button disabled={filter.page <= 1} onClick={() => setFilter({ ...filter, page: filter.page - 1 })}>Prev</button>
        <span>Trang {meta.page}</span>
        <button onClick={() => setFilter({ ...filter, page: filter.page + 1 })}>Next</button>
      </div>
    </section>
  );
}
