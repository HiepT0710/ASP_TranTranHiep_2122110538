import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import ActionMenu from "../../components/ActionMenu";
import SortFilterBar from "../../components/SortFilterBar";
import { getSellerOrders, sellerUpdateOrderStatus } from "../../services/apiService";
import { useChat } from "../../context/ChatContext";
import { useToast } from "../../context/ToastContext";

const ORDER_STATUS_LABELS = {
  Pending: "Chờ xác nhận",
  Preparing: "Đang chuẩn bị",
  Delivering: "Đang giao",
  Completed: "Đã hoàn thành",
  Cancelled: "Đã hủy",
};

const ORDER_STATUS_OPTIONS = [
  { value: "", label: "Tất cả trạng thái" },
  { value: "Pending", label: "Chờ xác nhận" },
  { value: "Preparing", label: "Đang chuẩn bị" },
  { value: "Delivering", label: "Đang giao" },
  { value: "Completed", label: "Đã hoàn thành" },
  { value: "Cancelled", label: "Đã hủy" },
];

export default function SellerOrdersPage() {
  const { pushToast } = useToast();
  const { openSupport, getUnreadCount } = useChat();
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState("");
  const [filter, setFilter] = useState({ page: 1, pageSize: 15, status: "", q: "", sortBy: "newest" });
  const [meta, setMeta] = useState({ page: 1, total: 0, totalPages: 1 });

  const loadData = async () => {
    setLoading(true);
    const data = await getSellerOrders(filter);
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

  const setStatus = async (id, status) => {
    try {
      await sellerUpdateOrderStatus(id, { status });
      const message = `Đã cập nhật trạng thái sang ${ORDER_STATUS_LABELS[status] || status}`;
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
      <div className="row" style={{ justifyContent: "space-between", alignItems: "center", flexWrap: "wrap" }}>
        <div>
          <p className="eyebrow">Quản lý đơn seller</p>
          <h2>Đơn hàng của quán</h2>
        </div>
        <div className="row" style={{ position: "relative", zIndex: 20 }}>
          <select value={filter.pageSize} onChange={(e) => setFilter({ ...filter, pageSize: Number(e.target.value), page: 1 })}>
            <option value={10}>10</option>
            <option value={15}>15</option>
            <option value={30}>30</option>
          </select>
        </div>
      </div>
      <SortFilterBar
        value={filter}
        onChange={setFilter}
        filters={[
          { key: "q", label: "Từ khóa", type: "input", placeholder: "Tìm theo khách / mã đơn" },
          { key: "status", label: "Trạng thái", type: "select", options: ORDER_STATUS_OPTIONS },
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
        <div className="panel">Đang tải đơn hàng...</div>
      ) : orders.length === 0 ? (
        <div className="panel soft-panel">Chưa có đơn nào phù hợp bộ lọc.</div>
      ) : (
        <table className="table">
          <thead><tr><th>ID</th><th>Khách</th><th>Tổng tiền</th><th>Trạng thái</th><th>Cập nhật</th><th>Chat</th><th>Chi tiết</th></tr></thead>
          <tbody>
            {orders.map((o) => (
              <tr key={o.id}>
                <td>{o.id}</td>
                <td>{o.username}</td>
                <td>{o.totalAmount}</td>
                <td><span className="badge">{ORDER_STATUS_LABELS[o.status] || o.status}</span></td>
                <td>
                  {o.status === "Cancelled" || o.status === "Completed" ? (
                    <span className="muted">Đã khóa</span>
                  ) : (
                    <ActionMenu
                      label="Cập nhật"
                      items={[
                        ...(o.status === "Pending" ? [{ label: ORDER_STATUS_LABELS.Preparing, onClick: () => setStatus(o.id, "Preparing") }] : []),
                        ...(o.status === "Pending" || o.status === "Preparing"
                          ? [{ label: ORDER_STATUS_LABELS.Delivering, onClick: () => setStatus(o.id, "Delivering") }]
                          : []),
                        ...(o.status === "Preparing" || o.status === "Delivering"
                          ? [{ label: ORDER_STATUS_LABELS.Completed, onClick: () => setStatus(o.id, "Completed") }]
                          : []),
                      ]}
                    />
                  )}
                </td>
                <td>
                  <button className="secondary" onClick={() => openSupport(o.id)}>Chat</button>
                  {getUnreadCount(o.id) > 0 && <span className="badge" style={{ marginLeft: 8 }}>{getUnreadCount(o.id)}</span>}
                </td>
                <td><Link to={`/orders/${o.id}`}>Xem</Link></td>
              </tr>
            ))}
          </tbody>
        </table>
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
