import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { getRestaurantDetails, getRestaurantReviews, resolveImageUrl } from "../services/apiService";
import { SkeletonCardGrid, StateMessage } from "../components/PageStates";

const stars = (value) => Array.from({ length: 5 }, (_, idx) => (idx < Math.round(value || 0) ? "★" : "☆")).join("");

export default function RestaurantDetailsPage() {
  const { id } = useParams();
  const [detail, setDetail] = useState(null);
  const [reviews, setReviews] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const [d, r] = await Promise.all([
          getRestaurantDetails(id),
          getRestaurantReviews({ restaurantId: id, pageSize: 6 }).catch(() => ({ items: [] })),
        ]);
        setDetail(d);
        setReviews(r.items || []);
      } catch (e) {
        setError(e?.response?.data?.message || "Không tải được chi tiết quán");
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [id]);

  if (loading) return <section className="page"><SkeletonCardGrid count={2} /></section>;
  if (error) return <section className="page"><StateMessage title="Không tải được quán" description={error} /></section>;
  if (!detail) return <section className="page"><StateMessage title="Không tìm thấy quán" /></section>;

  return (
    <section className="page">
      <div className="panel">
        {detail.coverImage ? <img src={resolveImageUrl(detail.coverImage)} alt={detail.name} style={{ width: "100%", height: 260, objectFit: "cover", borderRadius: 18, marginBottom: 12 }} /> : null}
        <div className="row" style={{ justifyContent: "space-between", alignItems: "flex-start" }}>
          <div>
            <p className="eyebrow">Chi tiết quán</p>
            <h2 style={{ marginTop: 0 }}>{detail.name}</h2>
            <p className="muted">{detail.address || "Chưa có địa chỉ"}</p>
            <p className="muted">SĐT: {detail.phone || "Chưa có"}</p>
          </div>
          <div style={{ textAlign: "right" }}>
            <div className="badge">{detail.avgRating ? `${Number(detail.avgRating).toFixed(1)}★` : "Chưa có đánh giá"}</div>
            <div className="badge">{detail.reviewCount || 0} đánh giá</div>
            <div className="badge">{detail.foodCount || 0} món</div>
          </div>
        </div>
      </div>

      <div className="cards" style={{ marginTop: 16 }}>
        <article className="panel"><b>Giờ mở cửa</b><p className="muted">Chưa có dữ liệu giờ mở cửa trong model hiện tại.</p></article>
        <article className="panel"><b>Khoảng cách</b><p className="muted">Có thể bổ sung sau bằng vị trí giao hàng / map.</p></article>
        <article className="panel"><b>Phí giao</b><p className="muted">Cấu hình từ backend chưa hiển thị ở màn này.</p></article>
        <article className="panel"><b>Đánh giá hiện tại</b><p className="muted">{detail.avgRating ? `${stars(detail.avgRating)} (${Number(detail.avgRating).toFixed(1)})` : "Chưa có sao"}</p></article>
      </div>

      <div className="panel" style={{ marginTop: 16 }}>
        <h3>Món phổ biến</h3>
        <p className="muted">Danh sách món bán chạy đang được hiển thị ở trang món ăn và có thể lọc theo quán.</p>
        <Link className="button secondary" to={`/foods?restaurantId=${detail.id}`}>Xem món của quán</Link>
      </div>

      <div className="panel" style={{ marginTop: 16 }}>
        <h3>Đánh giá gần đây</h3>
        {reviews.length === 0 ? (
          <p className="muted">Chưa có đánh giá nào.</p>
        ) : (
          <div className="cards">
            {reviews.map((r, index) => (
              <article key={`${index}-${r.createdAt}`} className="panel soft-panel">
                <div className="row" style={{ justifyContent: "space-between" }}>
                  <b>{r.username}</b>
                  <span className="badge">{stars(r.rating)}</span>
                </div>
                <p className="muted">{r.comment || "Không có nhận xét"}</p>
              </article>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}
