import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { cancelOrder, getMyOrders } from "../services/apiService";
import { useToast } from "../context/ToastContext";

export default function OrdersPage() {
  const { pushToast } = useToast();
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState("");
  const [filter, setFilter] = useState({ page: 1, pageSize: 10 });
  const [meta, setMeta] = useState({ page: 1, total: 0 });

  const loadData = async () => {
    setLoading(true);
    const data = await getMyOrders(filter);
    setOrders(data.items || []);
    setMeta({ page: data.page || filter.page, total: data.total || 0 });
    setLoading(false);
  };

  useEffect(() => {
    loadData();
  }, [filter.page, filter.pageSize]);

  const handleCancel = async (id) => {
    try {
      await cancelOrder(id, "Khách tự hủy");
      const message = "Đã hủy đơn";
      setMsg(message);
      pushToast(message, "info");
      loadData();
    } catch (error) {
      const message = error?.response?.data?.message || "Không hủy được";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  return (
    <section className="page">
      <div className="row" style={{ justifyContent: "space-between" }}>
        <div>
          <p className="eyebrow">Lịch sử đơn</p>
          <h2>Đơn hàng của tôi</h2>
        </div>
        <select value={filter.pageSize} onChange={(e) => setFilter({ ...filter, pageSize: Number(e.target.value), page: 1 })}>
          <option value={10}>10</option>
          <option value={20}>20</option>
        </select>
      </div>
      {msg && <p className="ok">{msg}</p>}
      {loading ? (
        <div className="panel">Đang tải danh sách đơn...</div>
      ) : orders.length === 0 ? (
        <div className="panel soft-panel">
          <h3>Chưa có đơn hàng</h3>
          <p className="muted">Bạn có thể quay lại trang món ăn để chọn món và đặt hàng.</p>
        </div>
      ) : (
        <div className="cards">
          {orders.map((o) => (
            <article key={o.id} className="panel">
              <div className="row" style={{ justifyContent: "space-between" }}>
                <b>Đơn #{o.id}</b>
                <span className="badge">{o.status}</span>
              </div>
              <p className="muted">Quán: {o.restaurantName}</p>
              <p>Tổng tiền: <b>{o.totalAmount}</b></p>
              <p>Thanh toán: {o.paymentStatus}</p>
              <div className="row">
                <Link to={`/orders/${o.id}`}>Chi tiết</Link>
                {o.canCancel && <button onClick={() => handleCancel(o.id)}>Hủy đơn</button>}
              </div>
            </article>
          ))}
        </div>
      )}
      <div className="row">
        <button disabled={filter.page <= 1} onClick={() => setFilter({ ...filter, page: filter.page - 1 })}>Prev</button>
        <span>Trang {meta.page}</span>
        <button onClick={() => setFilter({ ...filter, page: filter.page + 1 })}>Next</button>
      </div>
    </section>
  );
}
