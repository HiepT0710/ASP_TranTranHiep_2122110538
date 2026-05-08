import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { addToCart, getFoodDetails, getFoodReviews, getFoods, resolveImageUrl } from "../services/apiService";
import { SkeletonCardGrid } from "../components/PageStates";
import { InlineError } from "../components/LoadingError";

export default function FoodDetailsPage() {
  const { id } = useParams();
  const [food, setFood] = useState(null);
  const [reviews, setReviews] = useState([]);
  const [reviewPage, setReviewPage] = useState(1);
  const [reviewMeta, setReviewMeta] = useState({ page: 1, totalPages: 1 });
  const [reviewFilter, setReviewFilter] = useState("");
  const [related, setRelated] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [reviewError, setReviewError] = useState("");
  const [msg, setMsg] = useState("");

  const avgRating = useMemo(() => Number(food?.avgRating || 0), [food]);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      setLoading(true);
      setError("");
      setReviewError("");
      try {
        const f = await getFoodDetails(id);
        if (cancelled) return;
        setFood(f);

        const relatedRes = await getFoods({ restaurantId: f.restaurantId, page: 1, pageSize: 50 });
        if (!cancelled) {
          setRelated((relatedRes.items || []).filter((x) => x.id !== f.id).slice(0, 4));
        }

        try {
          const r = await getFoodReviews(id, { page: reviewPage, pageSize: 6, rating: reviewFilter || undefined });
          if (!cancelled) {
            setReviews(r.items || []);
            setReviewMeta({ page: r.page || reviewPage, totalPages: r.totalPages || 1 });
          }
        } catch (reviewErr) {
          if (!cancelled) {
            setReviews([]);
            setReviewMeta({ page: reviewPage, totalPages: 1 });
            setReviewError(reviewErr?.response?.data?.message || "Không tải được đánh giá món");
          }
        }
      } catch (e) {
        if (!cancelled) setError(e?.response?.data?.message || "Không tải được chi tiết món");
      } finally {
        if (!cancelled) setLoading(false);
      }
    };
    load();
    return () => { cancelled = true; };
  }, [id, reviewPage, reviewFilter]);

  const handleAdd = async () => {
    try {
      const res = await addToCart({ foodId: Number(id), quantity: 1 });
      setMsg(res.message || "Đã thêm vào giỏ");
    } catch (error) {
      setMsg(error?.response?.data?.message || "Không thêm được");
    }
  };

  if (loading) return <section className="page"><SkeletonCardGrid count={2} /></section>;
  if (error) return <section className="page"><InlineError message={error} onRetry={() => window.location.reload()} /></section>;
  if (!food) return <section className="page">Đang tải chi tiết món...</section>;

  return (
    <section className="page hero-card">
      <div className="split" style={{ alignItems: "start", gap: 24 }}>
        <div>
          {food.image && <img src={resolveImageUrl(food.image)} alt={food.name} style={{ width: "100%", maxHeight: 420, objectFit: "cover", borderRadius: 24, marginBottom: 16, border: "1px solid var(--border)" }} />}
          <p className="eyebrow">Chi tiết món</p>
          <h1 style={{ fontSize: 40, lineHeight: 1.1, marginBottom: 12 }}>{food.name}</h1>
          <p className="lead" style={{ maxWidth: 760 }}>{food.description || "Món ăn hấp dẫn, trình bày đẹp và sẵn sàng đặt hàng."}</p>
          <div className="row" style={{ flexWrap: "wrap", gap: 10, marginTop: 16 }}>
            <span className="badge">{food.restaurantName}</span>
            <span className="badge">{food.categoryName}</span>
            <span className="badge">{avgRating ? `${avgRating.toFixed(1)}★` : "Chưa có đánh giá"}</span>
            {food.isOnSale && <span className="badge">Sale -{food.salePercent}%</span>}
            <span className="badge">{food.stockQuantity > 0 ? "Còn hàng" : "Hết hàng"}</span>
          </div>
          <div className="panel" style={{ marginTop: 18, display: "grid", gap: 10, maxWidth: 420 }}>
            <div className="row" style={{ justifyContent: "space-between" }}>
              <span className="muted">Giá bán</span>
              <strong style={{ fontSize: 24 }}>{food.price}</strong>
            </div>
            <div className="row" style={{ justifyContent: "space-between" }}>
              <span className="muted">Tình trạng</span>
              <b>{food.isAvailable ? "Đang bán" : "Ngừng bán"}</b>
            </div>
            <div className="row" style={{ justifyContent: "space-between" }}>
              <span className="muted">Quán</span>
              <b>{food.restaurantName}</b>
            </div>
          </div>
          {msg && <p className="ok">{msg}</p>}
          <div className="row" style={{ marginTop: 18 }}>
            <button onClick={handleAdd}>Thêm vào giỏ</button>
            <Link to={`/restaurants/${food.restaurantId}`} className="button secondary">Xem quán</Link>
          </div>
        </div>
        <div className="panel soft-panel" style={{ minWidth: 280 }}>
          <h3>Thông tin nhanh</h3>
          <p className="muted">Được đánh giá bởi khách hàng sau khi hoàn tất đơn hàng.</p>
          <div className="stat-grid">
            <div className="stat-card"><span>★</span><strong>{avgRating ? avgRating.toFixed(1) : 0}</strong><p className="muted">Điểm trung bình</p></div>
            <div className="stat-card"><span>#</span><strong>{food.categoryName || "--"}</strong><p className="muted">Danh mục</p></div>
            <div className="stat-card"><span>{food.stockQuantity}</span><strong>Tồn kho</strong><p className="muted">Số lượng còn lại</p></div>
          </div>
        </div>
      </div>

      <h3 style={{ marginTop: 28 }}>Món cùng quán</h3>
      <div className="cards">
        {related.map((x) => (
          <article key={x.id} className="panel">
            {x.image && <img src={resolveImageUrl(x.image)} alt={x.name} style={{ width: "100%", height: 160, objectFit: "cover", borderRadius: 16, marginBottom: 12 }} />}
            <div className="row" style={{ justifyContent: "space-between" }}>
              <span className="badge">{x.categoryName}</span>
              {x.isOnSale && <span className="badge">Sale -{x.salePercent}%</span>}
            </div>
            <h4>{x.name}</h4>
            <p className="muted">{x.description || ""}</p>
            <Link to={`/foods/${x.id}`} className="link-btn">Xem chi tiết</Link>
          </article>
        ))}
      </div>

      <h3 style={{ marginTop: 28, fontSize: 28 }}>Đánh giá của khách hàng</h3>
      <div className="row" style={{ marginBottom: 12, flexWrap: "wrap", gap: 10 }}>
        <select value={reviewFilter} onChange={(e) => { setReviewFilter(e.target.value); setReviewPage(1); }} style={{ width: 160 }}>
          <option value="">Tất cả sao</option>
          {[5,4,3,2,1].map((star) => <option key={star} value={star}>{star} sao</option>)}
        </select>
        <span className="badge">Trang {reviewMeta.page} / {reviewMeta.totalPages}</span>
      </div>
      {reviewError && <InlineError message={reviewError} onRetry={() => window.location.reload()} />}
      <div className="cards">
        {reviews.length === 0 ? (
          <div className="panel soft-panel">Chưa có đánh giá.</div>
        ) : reviews.map((r, idx) => {
          let images = [];
          try {
            images = JSON.parse(r.imageUrlsJson || "[]").filter(Boolean);
          } catch {
            images = [];
          }
          return (
            <article key={idx} className="panel" style={{ display: "grid", gap: 12 }}>
              <div className="row" style={{ justifyContent: "space-between" }}>
                <b>{r.username}</b>
                <span className="badge">{r.rating}★</span>
              </div>
              <p className="muted" style={{ marginBottom: 0 }}>{r.comment || "Không có bình luận"}</p>
              {images.length > 0 && (
                <div className="cards" style={{ gridTemplateColumns: "repeat(auto-fit,minmax(130px,1fr))" }}>
                  {images.map((img, i) => (
                    <img key={`${idx}-${i}`} src={resolveImageUrl(img)} alt={`review-${i + 1}`} style={{ width: "100%", height: 130, objectFit: "cover", borderRadius: 14, border: "1px solid var(--border-color)" }} />
                  ))}
                </div>
              )}
            </article>
          );
        })}
      </div>
      {reviewMeta.totalPages > 1 && (
        <div className="row" style={{ marginTop: 12 }}>
          <button className="secondary" disabled={reviewPage <= 1} onClick={() => setReviewPage((p) => p - 1)}>Prev</button>
          <button className="secondary" disabled={reviewPage >= reviewMeta.totalPages} onClick={() => setReviewPage((p) => p + 1)}>Next</button>
        </div>
      )}
    </section>
  );
}
