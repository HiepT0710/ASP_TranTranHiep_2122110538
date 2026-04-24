import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { addToCart, getFoodDetails, getFoodReviews, getFoods, resolveImageUrl } from "../services/apiService";
import { SkeletonCardGrid } from "../components/PageStates";
import { InlineError } from "../components/LoadingError";

export default function FoodDetailsPage() {
  const { id } = useParams();
  const [food, setFood] = useState(null);
  const [reviews, setReviews] = useState([]);
  const [related, setRelated] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [msg, setMsg] = useState("");

  useEffect(() => {
    setLoading(true);
    setError("");
    Promise.all([getFoodDetails(id), getFoodReviews(id, { page: 1, pageSize: 10 }), getFoods({ restaurantId: undefined, page: 1, pageSize: 50 })])
      .then(([f, r, rel]) => {
        setFood(f);
        setReviews(r.items || []);
        setRelated((rel.items || []).filter((x) => x.restaurantId === f.restaurantId && x.id !== f.id).slice(0, 4));
      })
      .catch((e) => setError(e?.response?.data?.message || "Không tải được chi tiết món"))
      .finally(() => setLoading(false));
  }, [id]);

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
      <div className="split">
        <div>
          {food.image && <img src={resolveImageUrl(food.image)} alt={food.name} style={{ width: "100%", maxHeight: 360, objectFit: "cover", borderRadius: 22, marginBottom: 16, border: "1px solid var(--border)" }} />}
          <p className="eyebrow">Chi tiết món</p>
          <h1>{food.name}</h1>
          <p className="lead">{food.description || "Món ăn hấp dẫn, trình bày đẹp và sẵn sàng đặt hàng."}</p>
          <div className="row">
            <span className="badge">{food.restaurantName}</span>
            <span className="badge">{food.categoryName}</span>
            <span className="badge">{food.avgRating ? `${food.avgRating.toFixed(1)}★` : "Chưa có review"}</span>
            {food.isOnSale && <span className="badge">Sale -{food.salePercent}%</span>}
          </div>
          <div className="row">
            <strong>Giá: {food.price}</strong>
          </div>
          {msg && <p className="ok">{msg}</p>}
          <button onClick={handleAdd}>Thêm vào giỏ</button>
        </div>
        <div className="panel soft-panel">
          <h3>Thông tin bán hàng</h3>
          <p>Tồn kho: <b>{food.stockQuantity}</b></p>
          <p>Trạng thái: <b>{food.isAvailable ? "Đang bán" : "Ngừng bán"}</b></p>
          <p>Quán: <b>{food.restaurantName}</b></p>
        </div>
      </div>

      <h3 style={{ marginTop: 24 }}>Món cùng quán</h3>
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

      <h3 style={{ marginTop: 24 }}>Đánh giá gần đây</h3>
      <div className="cards">
        {reviews.map((r, idx) => (
          <article key={idx} className="panel">
            <div className="row" style={{ justifyContent: "space-between" }}>
              <b>{r.username}</b>
              <span className="badge">{r.rating}★</span>
            </div>
            <p className="muted">{r.comment || "Không có bình luận"}</p>
          </article>
        ))}
      </div>
    </section>
  );
}
