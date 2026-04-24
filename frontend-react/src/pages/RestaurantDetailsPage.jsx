import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getFoodCategories, getFoods, getRestaurantDetails, resolveImageUrl } from "../services/apiService";
import { SkeletonCardGrid } from "../components/PageStates";
import { InlineError } from "../components/LoadingError";

export default function RestaurantDetailsPage() {
  const { id } = useParams();
  const [restaurant, setRestaurant] = useState(null);
  const [foods, setFoods] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    setLoading(true);
    setError("");
    Promise.all([
      getRestaurantDetails(id),
      getFoods({ restaurantId: id, page: 1, pageSize: 8 }),
      getFoodCategories(id),
    ])
      .then(([r, f, c]) => {
        setRestaurant(r);
        setFoods(f.items || []);
        setCategories(c.items || []);
      })
      .catch((e) => setError(e?.response?.data?.message || "Không tải được chi tiết quán"))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <section className="page"><SkeletonCardGrid count={3} /></section>;
  if (error) return <section className="page"><InlineError message={error} onRetry={() => window.location.reload()} /></section>;
  if (!restaurant) return <section className="page">Đang tải chi tiết quán...</section>;

  const gallery = [restaurant.coverImage, restaurant.galleryImage1, restaurant.galleryImage2, restaurant.galleryImage3].filter(Boolean);

  return (
    <section className="page hero-card">
      <div className="split">
        <div>
          <p className="eyebrow">Chi tiết quán</p>
          <h1>{restaurant.name}</h1>
          <p className="lead">{restaurant.address || "Chưa có địa chỉ"}</p>
          <p>{restaurant.phone || "Chưa có số điện thoại"}</p>
          <div className="row">
            <span className="badge">{restaurant.foodCount || 0} món</span>
            <span className="badge">{restaurant.categoryCount || 0} danh mục</span>
            {restaurant.isOnSale && <span className="badge">Sale -{restaurant.salePercent}%</span>}
          </div>
          <div className="row">
            <Link to={`/foods?restaurantId=${restaurant.id}`}><button>Xem toàn bộ món</button></Link>
          </div>
        </div>
        <div className="panel soft-panel">
          <h3>Ảnh quán</h3>
          <div className="cards" style={{ gridTemplateColumns: "repeat(auto-fit, minmax(120px, 1fr))" }}>
            {gallery.length ? gallery.map((img, idx) => (
              <img key={idx} src={resolveImageUrl(img)} alt={`${restaurant.name} ${idx + 1}`} style={{ width: "100%", height: 110, objectFit: "cover", borderRadius: 14 }} />
            )) : <p className="muted">Chưa có ảnh quán</p>}
          </div>
        </div>
      </div>

      <div className="page" style={{ marginTop: 20 }}>
        <h3>Món bán chạy</h3>
        <div className="pill-list">
          {(foods.slice(0, 6) || []).map((f) => (
            <Link key={f.id} to={`/foods/${f.id}`} className="pill" style={{ textDecoration: "none" }}>
              {f.name}
            </Link>
          ))}
        </div>
      </div>

      <div className="page" style={{ marginTop: 20 }}>
        <h3>Món của quán</h3>
        <div className="cards">
          {foods.map((f) => (
            <article key={f.id} className="panel">
              {f.image && <img src={resolveImageUrl(f.image)} alt={f.name} style={{ width: "100%", height: 160, objectFit: "cover", borderRadius: 16, marginBottom: 12 }} />}
              <div className="row" style={{ justifyContent: "space-between" }}>
                <span className="badge">{f.categoryName}</span>
                {f.isOnSale && <span className="badge">Sale -{f.salePercent}%</span>}
              </div>
              <h4>{f.name}</h4>
              <p className="muted">{f.description || "Món ăn đang được cập nhật"}</p>
              <div className="card-actions">
                <Link to={`/foods/${f.id}`} className="link-btn">Xem chi tiết</Link>
              </div>
            </article>
          ))}
        </div>
      </div>

      <div className="page" style={{ marginTop: 20 }}>
        <h3>Danh mục</h3>
        <div className="pill-list">
          {categories.map((c) => <span key={c.id} className="pill">{c.name}</span>)}
        </div>
      </div>
    </section>
  );
}
