import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { getOrderChatMessages, getOrderDetails, getOrderStatusHistory, submitOrderReview } from "../services/apiService";
import { SkeletonCardGrid, StateMessage } from "../components/PageStates";

export default function OrderDetailsPage() {
  const { id } = useParams();
  const [detail, setDetail] = useState(null);
  const [history, setHistory] = useState([]);
  const [chat, setChat] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [msg, setMsg] = useState("");
  const [ratings, setRatings] = useState({});

  const loadData = async () => {
    setLoading(true);
    setError("");
    try {
      const [d, h, c] = await Promise.all([
        getOrderDetails(id),
        getOrderStatusHistory(id),
        getOrderChatMessages(id, { page: 1, pageSize: 100 }),
      ]);
      setDetail(d);
      setHistory(h.items || []);
      setChat(c.items || []);
    } catch (e) {
      setError(e?.response?.data?.message || "Không tải được chi tiết đơn");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [id]);

  const reviewable = useMemo(
    () => (detail?.details || []).filter((x) => !x.hasReview),
    [detail]
  );

  const submitReview = async () => {
    const items = reviewable
      .filter((x) => ratings[x.foodId]?.rating)
      .map((x) => ({
        foodId: x.foodId,
        rating: Number(ratings[x.foodId].rating),
        comment: ratings[x.foodId].comment || "",
      }));
    if (items.length === 0) {
      setMsg("Bạn cần nhập ít nhất 1 dòng review hợp lệ.");
      return;
    }
    const res = await submitOrderReview({ orderId: Number(id), items });
    setMsg(res.message || "Đã gửi đánh giá");
    loadData();
  };

  if (loading) return <section className="page"><SkeletonCardGrid count={2} /></section>;
  if (error) return <section className="page"><StateMessage title="Không tải được chi tiết đơn" description={error} action={<button onClick={loadData}>Thử lại</button>} /></section>;
  if (!detail) return <section className="page">Đang tải chi tiết đơn...</section>;

  return (
    <section className="page">
      <h2>Chi tiết đơn #{detail.id}</h2>
      <p>Quán: {detail.restaurantName}</p>
      <p>Trạng thái: {detail.status}</p>
      <p>Thanh toán: {detail.paymentStatus}</p>
      <p>Tổng tiền: {detail.totalAmount}</p>
      {msg && <p className="ok">{msg}</p>}

      <h3>Danh sách món</h3>
      <table className="table">
        <thead><tr><th>Món</th><th>SL</th><th>Giá</th><th>Tổng</th><th>Review</th></tr></thead>
        <tbody>
          {(detail.details || []).map((x) => (
            <tr key={x.foodId}>
              <td>{x.foodName}</td>
              <td>{x.quantity}</td>
              <td>{x.price}</td>
              <td>{x.lineTotal}</td>
              <td>{x.hasReview ? "Đã review" : "Chưa review"}</td>
            </tr>
          ))}
        </tbody>
      </table>

      {detail.canSubmitReview && (
        <>
          <h3>Gửi đánh giá món</h3>
          {reviewable.map((x) => (
            <div key={x.foodId} className="panel">
              <b>{x.foodName}</b>
              <div className="row">
                <input
                  type="number"
                  min="1"
                  max="5"
                  placeholder="Rating 1-5"
                  value={ratings[x.foodId]?.rating || ""}
                  onChange={(e) =>
                    setRatings((prev) => ({
                      ...prev,
                      [x.foodId]: { ...(prev[x.foodId] || {}), rating: e.target.value },
                    }))
                  }
                />
                <input
                  placeholder="Nhận xét"
                  value={ratings[x.foodId]?.comment || ""}
                  onChange={(e) =>
                    setRatings((prev) => ({
                      ...prev,
                      [x.foodId]: { ...(prev[x.foodId] || {}), comment: e.target.value },
                    }))
                  }
                />
              </div>
            </div>
          ))}
          <button onClick={submitReview}>Gửi review</button>
        </>
      )}

      <h3>Lịch sử trạng thái</h3>
      <table className="table">
        <thead><tr><th>From</th><th>To</th><th>Vai trò</th><th>Ghi chú</th><th>Thời gian</th></tr></thead>
        <tbody>
          {history.map((x, idx) => (
            <tr key={idx}>
              <td>{x.fromStatus}</td>
              <td>{x.toStatus}</td>
              <td>{x.actorRole}</td>
              <td>{x.note}</td>
              <td>{x.createdAt}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <h3>Chat messages</h3>
      <div className="chat-box">
        {chat.map((m) => (
          <div key={m.id} className="chat-item">
            <b>{m.username}</b>: {m.message}
            <div className="muted">{m.createdAt}</div>
          </div>
        ))}
      </div>
      <p className="muted">* Khung chat realtime gửi tin nhắn qua SignalR hub sẽ tiếp tục mở rộng ở bước sau.</p>
    </section>
  );
}
