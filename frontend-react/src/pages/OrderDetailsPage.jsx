import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { api } from "../api";
import { cancelOrder, getOrderDetails, getOrderStatusHistory, submitOrderReview, submitRestaurantReview } from "../services/apiService";
import { SkeletonCardGrid, StateMessage } from "../components/PageStates";
import ReportButton from "../components/ReportButton";
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

const STATUS_META = {
  Pending: { label: "Chờ xác nhận", tone: "neutral" },
  Preparing: { label: "Đang chuẩn bị", tone: "warning" },
  Delivering: { label: "Đang giao", tone: "info" },
  Completed: { label: "Đã hoàn thành", tone: "success" },
  Cancelled: { label: "Đã hủy", tone: "danger" },
};

const ORDER_TIMELINE = ["Pending", "Preparing", "Delivering", "Completed"];

function renderStars(value) {
  return Array.from({ length: 5 }, (_, idx) => (idx < value ? "★" : "☆")).join("");
}

export default function OrderDetailsPage() {
  const { id } = useParams();
  const { pushToast } = useToast();
  const { openSupport } = useChat();
  const [detail, setDetail] = useState(null);
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [msg, setMsg] = useState("");
  const [ratings, setRatings] = useState({});
  const [restaurantRating, setRestaurantRating] = useState(5);
  const [restaurantComment, setRestaurantComment] = useState("");
  const [foodReviewImages, setFoodReviewImages] = useState([]);
  const [foodReviewPreviewUrls, setFoodReviewPreviewUrls] = useState([]);
  const [restaurantReviewImages, setRestaurantReviewImages] = useState([]);
  const [restaurantReviewPreviewUrls, setRestaurantReviewPreviewUrls] = useState([]);
  const [cancelReason, setCancelReason] = useState("");

  const loadData = async () => {
    setLoading(true);
    setError("");
    try {
      const [d, h] = await Promise.all([getOrderDetails(id), getOrderStatusHistory(id)]);
      setDetail(d);
      setHistory(h.items || []);
    } catch (e) {
      setError(e?.response?.data?.message || "Không tải được chi tiết đơn");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadData(); }, [id]);

  const reviewable = useMemo(() => (detail?.details || []).filter((x) => !x.hasReview), [detail]);

  const makePreviewHandler = (setFiles, setUrls) => (event) => {
    const files = Array.from(event.target.files || []).slice(0, 5);
    setFiles(files);
    const urls = files.map((file) => URL.createObjectURL(file));
    setUrls((current) => {
      current.forEach((url) => URL.revokeObjectURL(url));
      return urls;
    });
  };

  const onPickFoodReviewImages = makePreviewHandler(setFoodReviewImages, setFoodReviewPreviewUrls);
  const onPickRestaurantReviewImages = makePreviewHandler(setRestaurantReviewImages, setRestaurantReviewPreviewUrls);

  useEffect(() => () => {
    foodReviewPreviewUrls.forEach((url) => URL.revokeObjectURL(url));
    restaurantReviewPreviewUrls.forEach((url) => URL.revokeObjectURL(url));
  }, [foodReviewPreviewUrls, restaurantReviewPreviewUrls]);

  const submitReview = async () => {
    const items = reviewable
      .filter((x) => ratings[x.foodId]?.rating)
      .map((x) => ({ foodId: x.foodId, rating: Number(ratings[x.foodId].rating), comment: ratings[x.foodId].comment || "" }));
    if (items.length === 0) {
      setMsg("Bạn cần nhập ít nhất 1 món có đánh giá hợp lệ.");
      return;
    }
    try {
      const imagePayload = await Promise.all(foodReviewImages.map(async (file) => {
        const formData = new FormData();
        formData.append("file", file);
        const res = await api.post("/Upload/ReviewImage", formData, { headers: { "Content-Type": "multipart/form-data" } });
        return res.data.url;
      }));
      const itemsWithImages = items.map((item) => ({ ...item, images: imagePayload }));
      const res = await submitOrderReview({ orderId: Number(id), items: itemsWithImages });
      setMsg(res.message || "Đã gửi đánh giá");
      pushToast(res.message || "Đã gửi đánh giá", "success");
      loadData();
    } catch (e) {
      const message = e?.response?.data?.message || e?.response?.data?.detail || e?.message || "Không gửi được đánh giá";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  const ratingSummary = useMemo(() => {
    const values = Object.values(ratings).map((item) => Number(item?.rating || 0)).filter((value) => value >= 1 && value <= 5);
    if (values.length === 0) return { count: 0, avg: 0 };
    const avg = values.reduce((sum, value) => sum + value, 0) / values.length;
    return { count: values.length, avg };
  }, [ratings]);

  const cancelDeadline = useMemo(() => (detail?.cancelDeadline ? new Date(detail.cancelDeadline) : null), [detail?.cancelDeadline]);
  const canCancel = Boolean(detail?.canCancel);
  const cancelHint = !cancelDeadline ? "" : canCancel ? `Bạn có thể hủy trước ${formatDateTime(cancelDeadline)}.` : "Đã quá thời gian cho phép hủy đơn.";

  if (loading) return <section className="page"><SkeletonCardGrid count={2} /></section>;
  if (error) return <section className="page"><StateMessage title="Không tải được chi tiết đơn" description={error} action={<button onClick={loadData}>Thử lại</button>} /></section>;
  if (!detail) return <section className="page">Đang tải chi tiết đơn...</section>;

  return (
    <section className="page">
      <h2>Chi tiết đơn #{detail.id}</h2>
      <div className="panel">
        <div className="row" style={{ justifyContent: "space-between", alignItems: "flex-start" }}>
          <div>
            <p className="muted" style={{ marginBottom: 6 }}>Quán: <b>{detail.restaurantName}</b></p>
            <p className="muted" style={{ marginBottom: 6 }}>Thanh toán: <b>{PAYMENT_STATUS_LABELS[detail.paymentStatus] || detail.paymentStatus}</b></p>
            <p className="muted" style={{ marginBottom: 6 }}>Tổng tiền: <b>{detail.totalAmount}</b></p>
            <p className="muted" style={{ marginBottom: 0 }}>Hạn hủy đơn: <b>{formatDateTime(cancelDeadline)}</b></p>
          </div>
          <div className="row" style={{ gap: 8, alignItems: "flex-start", margin: 0 }}>
            <span className={`badge ${STATUS_META[detail.status]?.tone || ""}`}>{STATUS_META[detail.status]?.label || detail.status}</span>
            {canCancel ? (
              <button
                type="button"
                className="secondary"
                onClick={async () => {
                  try {
                    const res = await cancelOrder(detail.id, cancelReason.trim() || "Khách yêu cầu hủy đơn");
                    setMsg(res.message || "Đã hủy đơn");
                    pushToast(res.message || "Đã hủy đơn", "success");
                    loadData();
                  } catch (e) {
                    const message = e?.response?.data?.message || e?.message || "Không hủy được đơn";
                    setMsg(message);
                    pushToast(message, "error");
                  }
                }}
              >
                Hủy đơn
              </button>
            ) : (
              <button type="button" className="secondary" disabled title={cancelHint}>Không thể hủy</button>
            )}
          </div>
        </div>
        <p className="muted" style={{ marginTop: 12, marginBottom: 0 }}>{cancelHint || ""}</p>
        {canCancel && (
          <div style={{ marginTop: 12 }}>
            <textarea rows={3} value={cancelReason} onChange={(e) => setCancelReason(e.target.value)} placeholder="Lý do hủy đơn (không bắt buộc)" style={{ width: "100%", minHeight: 90 }} />
          </div>
        )}
      </div>
      {msg && <p className="ok">{msg}</p>}

      <h3>Tiến trình đơn hàng</h3>
      <div className="panel" style={{ display: "grid", gap: 12 }}>
        {ORDER_TIMELINE.map((step) => {
          const currentIndex = ORDER_TIMELINE.indexOf(detail.status);
          const stepIndex = ORDER_TIMELINE.indexOf(step);
          const isDone = currentIndex >= stepIndex && detail.status !== "Cancelled";
          const isCurrent = detail.status === step;
          return (
            <div key={step} className="row" style={{ justifyContent: "space-between", padding: "8px 0", borderBottom: step !== ORDER_TIMELINE[ORDER_TIMELINE.length - 1] ? "1px solid var(--border-color)" : "none" }}>
              <div>
                <b>{ORDER_STATUS_LABELS[step] || step}</b>
                <div className="muted" style={{ fontSize: 13 }}>{isCurrent ? "Đang ở trạng thái này" : isDone ? "Đã hoàn tất bước này" : "Chưa đến bước này"}</div>
              </div>
              <span className={`badge ${isCurrent ? "success" : isDone ? "" : "soft"}`}>{isCurrent ? "Hiện tại" : isDone ? "✓" : "..."}</span>
            </div>
          );
        })}
        {detail.status === "Cancelled" && <div className="badge danger">Đơn đã bị hủy</div>}
      </div>

      <h3>Danh sách món</h3>
      <table className="table">
        <thead><tr><th>Món</th><th>SL</th><th>Giá</th><th>Tổng</th><th>Đánh giá</th></tr></thead>
        <tbody>
          {(detail.details || []).map((x) => (
            <tr key={x.foodId}>
              <td>{x.foodName}</td>
              <td>{x.quantity}</td>
              <td>{x.price}</td>
              <td>{x.lineTotal}</td>
              <td>{x.hasReview ? "Đã đánh giá" : "Chưa đánh giá"}</td>
            </tr>
          ))}
        </tbody>
      </table>

      {detail.canSubmitReview && (
        <>
          <h3>Gửi đánh giá quán</h3>
          <div className="panel" style={{ display: "grid", gap: 12, marginBottom: 18 }}>
            <div className="row" style={{ gap: 6, flexWrap: "wrap" }}>
              {[1, 2, 3, 4, 5].map((star) => (
                <button key={star} type="button" className="link-btn" onClick={() => setRestaurantRating(star)} style={{ fontSize: 24, lineHeight: 1, color: star <= restaurantRating ? "#f5a623" : "#cbd5e1" }} aria-label={`${star} sao quán`}>
                  ★
                </button>
              ))}
            </div>
            <textarea placeholder="Nhận xét về thái độ phục vụ, thời gian chuẩn bị, chất lượng quán..." value={restaurantComment} onChange={(e) => setRestaurantComment(e.target.value)} rows={4} style={{ minHeight: 120, resize: "vertical" }} />
            <div className="panel soft-panel" style={{ marginBottom: 0 }}>
              <b>Ảnh đánh giá quán</b>
              <input type="file" accept="image/*" multiple onChange={onPickRestaurantReviewImages} style={{ display: "block", marginTop: 10 }} />
              {restaurantReviewPreviewUrls.length > 0 && (
                <div className="cards" style={{ marginTop: 12 }}>
                  {restaurantReviewPreviewUrls.map((url, index) => (
                    <img key={url} src={url} alt={`preview-${index + 1}`} style={{ width: "100%", height: 140, objectFit: "cover", borderRadius: 14, border: "1px solid var(--border-color)" }} />
                  ))}
                </div>
              )}
            </div>
            <button type="button" onClick={async () => {
              try {
                const imagePayload = await Promise.all(restaurantReviewImages.map(async (file) => {
                  const formData = new FormData();
                  formData.append("file", file);
                  const res = await api.post("/Upload/ReviewImage", formData, { headers: { "Content-Type": "multipart/form-data" } });
                  return res.data.url;
                }));
                const res = await submitRestaurantReview({ orderId: Number(id), rating: restaurantRating, comment: restaurantComment, images: imagePayload });
                pushToast(res.message || "Đã gửi đánh giá quán", "success");
                setMsg(res.message || "Đã gửi đánh giá quán");
                loadData();
              } catch (e) {
                const message = e?.response?.data?.message || e?.response?.data?.detail || e?.message || "Không gửi được đánh giá quán";
                pushToast(message, "error");
                setMsg(message);
              }
            }}>Gửi đánh giá quán</button>
          </div>
          <h3>Gửi đánh giá món</h3>
          <div className="panel soft-panel" style={{ marginBottom: 12 }}>
            <b>Tổng quan đánh giá hiện tại</b>
            <p className="muted" style={{ marginBottom: 0 }}>Bạn đã chọn <b>{ratingSummary.count}</b> món có đánh giá với điểm trung bình <b>{ratingSummary.avg ? ratingSummary.avg.toFixed(1) : 0}/5</b>.</p>
          </div>
          <div className="panel" style={{ marginBottom: 12 }}>
            <b>Ảnh đánh giá</b>
            <input type="file" accept="image/*" multiple onChange={onPickFoodReviewImages} style={{ display: "block", marginTop: 10 }} />
            <p className="muted" style={{ marginTop: 8, marginBottom: 0 }}>Tối đa 5 ảnh. Có thể xem trước trước khi gửi.</p>
            {foodReviewPreviewUrls.length > 0 && (
              <div className="cards" style={{ marginTop: 12 }}>
                {foodReviewPreviewUrls.map((url, index) => (
                  <img key={url} src={url} alt={`preview-${index + 1}`} style={{ width: "100%", height: 140, objectFit: "cover", borderRadius: 14, border: "1px solid var(--border-color)" }} />
                ))}
              </div>
            )}
          </div>
          {reviewable.map((x) => {
            const selected = Number(ratings[x.foodId]?.rating || 0);
            return (
              <div key={x.foodId} className="panel" style={{ display: "grid", gap: 12 }}>
                <div className="row" style={{ justifyContent: "space-between" }}>
                  <b>{x.foodName}</b>
                  <span className="badge">Chưa đánh giá</span>
                </div>
                <div className="panel soft-panel" style={{ display: "grid", gap: 10 }}>
                  <div className="row" style={{ gap: 6, flexWrap: "wrap" }}>
                    {[1, 2, 3, 4, 5].map((star) => (
                      <button key={star} type="button" className="link-btn" onClick={() => setRatings((prev) => ({ ...prev, [x.foodId]: { ...(prev[x.foodId] || {}), rating: star } }))} style={{ fontSize: 24, lineHeight: 1, color: star <= selected ? "#f5a623" : "#cbd5e1" }} aria-label={`${star} sao`}>
                        ★
                      </button>
                    ))}
                  </div>
                  <div className="muted" style={{ fontSize: 13 }}>{selected ? `Bạn đã chọn ${renderStars(selected)}` : "Chạm vào các ngôi sao để chọn số sao"}</div>
                </div>
                <textarea placeholder="Viết nhận xét của bạn về món ăn ở đây..." value={ratings[x.foodId]?.comment || ""} onChange={(e) => setRatings((prev) => ({ ...prev, [x.foodId]: { ...(prev[x.foodId] || {}), comment: e.target.value } }))} rows={4} style={{ minHeight: 120, resize: "vertical" }} />
              </div>
            );
          })}
          <button onClick={submitReview}>Gửi đánh giá</button>
        </>
      )}

      <h3>Lịch sử trạng thái</h3>
      <div className="panel" style={{ display: "grid", gap: 10 }}>
        {history.length === 0 ? (
          <div className="muted">Chưa có lịch sử trạng thái.</div>
        ) : history.map((x, idx) => (
          <div key={idx} className="row" style={{ justifyContent: "space-between", alignItems: "flex-start", padding: "10px 0", borderBottom: idx < history.length - 1 ? "1px solid var(--border-color)" : "none" }}>
            <div>
              <div className="row" style={{ gap: 8, marginBottom: 4 }}>
                <span className="badge">{ORDER_STATUS_LABELS[x.fromStatus] || x.fromStatus}</span>
                <span className="muted">→</span>
                <span className="badge">{ORDER_STATUS_LABELS[x.toStatus] || x.toStatus}</span>
              </div>
              <div className="muted" style={{ fontSize: 13 }}>Vai trò: <b>{x.actorRole}</b>{x.note ? ` · ${x.note}` : ""}</div>
            </div>
            <span className="badge soft">{formatDateTime(x.createdAt)}</span>
          </div>
        ))}
      </div>

      <div className="row" style={{ marginTop: 18 }}>
        <button type="button" className="secondary" onClick={() => openSupport(detail.id, "admin")}>Liên hệ admin</button>
        <ReportButton targetType="Order" targetId={detail.id} label="Báo cáo đơn hàng" />
        <ReportButton targetType="Restaurant" targetId={detail.restaurantId} label="Báo cáo quán" />
      </div>
      <p className="muted">* Hệ thống chat realtime dùng SignalR để gửi thông báo hỗ trợ giữa khách, seller và admin.</p>
    </section>
  );
}
