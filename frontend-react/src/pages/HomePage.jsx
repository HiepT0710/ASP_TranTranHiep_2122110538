import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getBestFoods, getFoods, getRestaurants, getSaleRestaurants, resolveImageUrl } from "../services/apiService";
import { SkeletonCardGrid } from "../components/PageStates";

export default function HomePage() {
  const [featuredRestaurants, setFeaturedRestaurants] = useState([]);
  const [bestFoods, setBestFoods] = useState([]);
  const [saleFoods, setSaleFoods] = useState([]);
  const [saleRestaurants, setSaleRestaurants] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    Promise.all([
      getRestaurants({ page: 1, pageSize: 6, sortBy: "rating_desc" }).catch(() => ({ items: [] })),
      getBestFoods(6).catch(() => ({ items: [] })),
      getFoods({ page: 1, pageSize: 12, sortBy: "rating_desc" }).catch(() => ({ items: [] })),
      getSaleRestaurants().catch(() => ({ items: [] })),
    ]).then(([restaurantsRes, bestRes, foodsRes, saleRes]) => {
      if (!mounted) return;
      const foods = foodsRes.items || [];
      setFeaturedRestaurants(restaurantsRes.items || []);
      setBestFoods(bestRes.items || []);
      setSaleFoods(foods.filter((item) => item.isOnSale).slice(0, 6));
      setSaleRestaurants((saleRes.items || []).slice(0, 6));
      setLoading(false);
    }).catch(() => {
      if (mounted) setLoading(false);
    });
    return () => { mounted = false; };
  }, []);

  return (
    <section className="page hero-card home-page">
      <div className="split hero-split" style={{ alignItems: "center" }}>
        <div>
          <p className="eyebrow">FoodOrder Platform</p>
          <h1>Đặt món nhanh, tìm quán rõ, ưu tiên nội dung quan trọng</h1>
          <p className="lead">Nổi bật quán có đánh giá cao, món bán chạy và các khu vực đang giảm giá.</p>
          <div className="row">
            <Link to="/restaurants" className="button">Xem quán ăn</Link>
            <Link to="/foods" className="button secondary">Xem món ăn</Link>
          </div>
        </div>
        <div className="panel hero-summary-panel">
          <p className="eyebrow">Tổng quan</p>
          <div className="stat-grid">
            <div className="stat-card"><span>Quán nổi bật</span><strong>{featuredRestaurants.length}</strong></div>
            <div className="stat-card"><span>Món bán chạy</span><strong>{bestFoods.length}</strong></div>
            <div className="stat-card"><span>Món đang sale</span><strong>{saleFoods.length}</strong></div>
            <div className="stat-card"><span>Quán đang sale</span><strong>{saleRestaurants.length}</strong></div>
          </div>
        </div>
      </div>

      <section className="home-section">
        <div className="home-section-header"><h2>Quán ăn nổi bật</h2></div>
        {loading ? <SkeletonCardGrid count={3} /> : <div className="cards">{featuredRestaurants.map((r) => (<article key={r.id} className="panel home-card">{r.coverImage ? <img src={resolveImageUrl(r.coverImage)} alt={r.name} className="home-card-image" /> : <div className="home-card-image empty">Chưa có ảnh</div>}<div className="row compact"><span className="badge">{r.avgRating ? `${Number(r.avgRating).toFixed(1)}★` : "Chưa có đánh giá"}</span>{r.isOnSale && <span className="badge">Sale -{r.salePercent}%</span>}</div><h3>{r.name}</h3><p className="muted">{r.address || "Chưa cập nhật địa chỉ"}</p><Link to={`/restaurants/${r.id}`} className="button secondary">Xem chi tiết</Link></article>))}</div>}
      </section>

      <section className="home-section">
        <div className="home-section-header"><h2>Món ăn bán chạy</h2></div>
        {loading ? <SkeletonCardGrid count={3} /> : <div className="cards">{bestFoods.map((f) => (<article key={f.id} className="panel home-card">{f.image ? <img src={resolveImageUrl(f.image)} alt={f.name} className="home-card-image" /> : <div className="home-card-image empty">Chưa có ảnh</div>}<span className="badge">Bán chạy</span><h3>{f.name}</h3><p className="muted">{f.restaurantName}</p><Link to={`/foods/${f.id}`} className="button secondary">Xem chi tiết</Link></article>))}</div>}
      </section>

      <section className="home-section">
        <div className="home-section-header"><h2>Món ăn đang sale</h2></div>
        {loading ? <SkeletonCardGrid count={3} /> : <div className="cards">{saleFoods.map((f) => (<article key={f.id} className="panel home-card">{f.image ? <img src={resolveImageUrl(f.image)} alt={f.name} className="home-card-image" /> : <div className="home-card-image empty">Chưa có ảnh</div>}<span className="badge">Sale -{f.salePercent}%</span><h3>{f.name}</h3><p className="muted">{f.restaurantName}</p><Link to={`/foods/${f.id}`} className="button secondary">Xem chi tiết</Link></article>))}</div>}
      </section>

      <section className="home-section">
        <div className="home-section-header"><h2>Quán đang sale</h2></div>
        {loading ? <SkeletonCardGrid count={3} /> : <div className="cards">{saleRestaurants.map((r) => (<article key={r.id} className="panel home-card">{r.coverImage ? <img src={resolveImageUrl(r.coverImage)} alt={r.name} className="home-card-image" /> : <div className="home-card-image empty">Chưa có ảnh</div>}<span className="badge">Sale -{r.salePercent}%</span><h3>{r.name}</h3><p className="muted">{r.address || "Chưa cập nhật địa chỉ"}</p><Link to={`/restaurants/${r.id}`} className="button secondary">Xem chi tiết</Link></article>))}</div>}
      </section>
    </section>
  );
}
