import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import ActionMenu from "../../components/ActionMenu";
import { getSellerOrders, sellerUpdateOrderStatus } from "../../services/apiService";
import { useToast } from "../../context/ToastContext";

export default function SellerOrdersPage() {
  const { pushToast } = useToast();
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState("");
  const [filter, setFilter] = useState({ page: 1, pageSize: 15, status: "" });
  const [meta, setMeta] = useState({ page: 1, total: 0 });

  const loadData = async () => {
    setLoading(true);
    const data = await getSellerOrders(filter);
    setOrders(data.items || []);
    setMeta({ page: data.page || filter.page, total: data.total || 0 });
    setLoading(false);
  };

  useEffect(() => {
    loadData();
  }, [filter.page, filter.pageSize, filter.status]);

  const setStatus = async (id, status) => {
    try {
      await sellerUpdateOrderStatus(id, { status });
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
          <p className="eyebrow">Quản lý đơn seller</p>
          <h2>Đơn hàng của quán</h2>
        </div>
        <div className="row" style={{ position: "relative", zIndex: 20 }}>
          <select value={filter.status} onChange={(e) => setFilter({ ...filter, status: e.target.value, page: 1 })}>
            <option value="">Tất cả trạng thái</option>
            <option value="Pending">Pending</option>
            <option value="Preparing">Preparing</option>
            <option value="Delivering">Delivering</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
          <select value={filter.pageSize} onChange={(e) => setFilter({ ...filter, pageSize: Number(e.target.value), page: 1 })}>
            <option value={10}>10</option>
            <option value={15}>15</option>
            <option value={30}>30</option>
          </select>
        </div>
      </div>
      {msg && <p className="ok">{msg}</p>}
      {loading ? (
        <div className="panel">Đang tải đơn hàng...</div>
      ) : orders.length === 0 ? (
        <div className="panel soft-panel">Chưa có đơn nào phù hợp bộ lọc.</div>
      ) : (
        <table className="table">
          <thead><tr><th>ID</th><th>Khách</th><th>Tổng tiền</th><th>Trạng thái</th><th>Cập nhật</th><th>Chi tiết</th></tr></thead>
          <tbody>
            {orders.map((o) => (
              <tr key={o.id}>
                <td>{o.id}</td>
                <td>{o.username}</td>
                <td>{o.totalAmount}</td>
                <td><span className="badge">{o.status}</span></td>
                <td>
                  <ActionMenu
                    label="Cập nhật"
                    items={[
                      { label: "Preparing", onClick: () => setStatus(o.id, "Preparing") },
                      { label: "Delivering", onClick: () => setStatus(o.id, "Delivering") },
                      { label: "Completed", onClick: () => setStatus(o.id, "Completed") },
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
