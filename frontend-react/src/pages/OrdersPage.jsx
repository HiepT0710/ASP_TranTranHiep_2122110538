import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import SortFilterBar from "../components/SortFilterBar";
import { cancelOrder, getMyOrders } from "../services/apiService";
import { useToast } from "../context/ToastContext";
import { useChat } from "../context/ChatContext";
import { formatDateTime } from "../utils/dateTime";

const ORDER_STATUS_LABELS = {
  Pending: "Chờ xác nhận",
  Preparing: "Đang chuẩn bị",
  Delivering: "Đang giao",
  Completed: "Đã hoàn thành",
  Cancelled: "Đã hủy",
};

const PAYMENT_STATUS_LABELS = {
  Pending: "Chưa thanh toán",
  Paid: "Đã thanh toán",
  Failed: "Thanh toán thất bại",
  Refunded: "Đã hoàn tiền",
};

export default function OrdersPage() {
  const { pushToast } = useToast();
  const { openSupport, hydrateThread, reopenBubble } = useChat();
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState("");
  const [filter, setFilter] = useState({ page: 1, pageSize: 10, status: "", q: "", sortBy: "newest" });
  const [meta, setMeta] = useState({ page: 1, total: 0, totalPages: 1 });

  const loadData = async () => {
    setLoading(true);
    const data = await getMyOrders(filter);
    const total = data.total || 0;
    const totalPages = Math.max(1, Math.ceil(total / filter.pageSize));
    const page = Math.min(data.page || filter.page, totalPages);
    setOrders(data.items || []);
    setMeta({ page, total, totalPages });
    setFilter((current) => (current.page > totalPages ? { ...current, page: totalPages } : current));
    setLoading(false);
  };

  useEffect(() => {
    loadData();
  }, [filter.page, filter.pageSize, filter.status, filter.q, filter.sortBy]);

  const handleCancel = async (id) => {
    try {
      await cancelOrder(id, "Khách tự hủy");
      const message = "Đã hủy đơn";
      setMsg(message);
      pushToast(message, "info");
      await loadData();
    } catch (error) {
      const message = error?.response?.data?.message || "Không hủy được";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  const handleChat = async (orderId) => {
    await hydrateThread(orderId);
    openSupport(orderId);
    reopenBubble(orderId);
  };

  return (
    <section className="page">
      <div className="row" style={{ justifyContent: "space-between", alignItems: "center", flexWrap: "wrap" }}>
        <div>
          <p className="eyebrow">Lịch sử đơn</p>
          <h2>Đơn hàng của tôi</h2>
        </div>
        <select value={filter.pageSize} onChange={(e) => setFilter({ ...filter, pageSize: Number(e.target.value), page: 1 })}>
          <option value={10}>10</option>
          <option value={20}>20</option>
        </select>
      </div>
      <SortFilterBar
        value={filter}
        onChange={setFilter}
        filters={[
          { key: "q", label: "Từ khóa", type: "input", placeholder: "Tìm theo mã đơn / quán / trạng thái" },
          { key: "status", label: "Trạng thái", type: "select", options: [{ value: "", label: "Tất cả trạng thái" }, { value: "Pending", label: "Chờ xác nhận" }, { value: "Preparing", label: "Đang chuẩn bị" }, { value: "Delivering", label: "Đang giao" }, { value: "Completed", label: "Đã hoàn thành" }, { value: "Cancelled", label: "Đã hủy" }] },
        ]}
        sortOptions={[
          { value: "newest", label: "Mới nhất" },
          { value: "oldest", label: "Cũ nhất" },
          { value: "total_desc", label: "Tổng tiền cao nhất" },
          { value: "total_asc", label: "Tổng tiền thấp nhất" },
        ]}
      />
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
                <span className="badge">{ORDER_STATUS_LABELS[o.status] || o.status}</span>
              </div>
              <p className="muted">Quán: {o.restaurantName}</p>
              <p>Tổng tiền: <b>{o.totalAmount}</b></p>
              <p>Thanh toán: {PAYMENT_STATUS_LABELS[o.paymentStatus] || o.paymentStatus}</p>
              <p className="muted" style={{ marginTop: 8, marginBottom: 0 }}>
                Hạn hủy: <b>{formatDateTime(o.cancelDeadline)}</b>
              </p>
              <div className="row">
                <Link to={`/orders/${o.id}`} className="button secondary">Chi tiết</Link>
                <button className="secondary" onClick={() => handleChat(o.id)}>Chat</button>
                <button className="secondary" disabled={!o.canCancel} title={!o.canCancel ? "Đã quá thời gian cho phép hủy đơn" : ""} onClick={() => handleCancel(o.id)}>
                  {o.canCancel ? "Hủy đơn" : "Không thể hủy"}
                </button>
              </div>
            </article>
          ))}
        </div>
      )}
      {meta.totalPages > 1 && (
        <div className="row">
          <button className="secondary" disabled={filter.page <= 1} onClick={() => setFilter({ ...filter, page: filter.page - 1 })}>Prev</button>
          <span className="badge">Trang {meta.page} / {meta.totalPages}</span>
          <button className="secondary" disabled={filter.page >= meta.totalPages} onClick={() => setFilter({ ...filter, page: filter.page + 1 })}>Next</button>
        </div>
      )}
    </section>
  );
}
