import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { getBestFoods, getFoods, getSaleRestaurants, resolveImageUrl } from "../services/apiService";
import { useAuth } from "../context/AuthContext";
import { SkeletonCardGrid } from "../components/PageStates";

export default function HomePage() {
  const { user } = useAuth();
  const roleLabel = useMemo(() => user?.role || "Guest", [user]);
  const [saleRestaurants, setSaleRestaurants] = useState([]);
  const [saleFoods, setSaleFoods] = useState([]);
  const [bestFoods, setBestFoods] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    Promise.all([
      getSaleRestaurants(),
      getFoods({ page: 1, pageSize: 6 }),
      getBestFoods(6),
    ]).then(([restaurantsRes, foodsRes, bestRes]) => {
      if (!mounted) return;
      setSaleRestaurants(restaurantsRes.items || []);
      setSaleFoods((foodsRes.items || []).filter((x) => x.isOnSale));
      setBestFoods(bestRes.items || []);
      setLoading(false);
    }).catch(() => {
      if (mounted) setLoading(false);
    });
    return () => {
      mounted = false;
    };
  }, []);

  return (
    <section className="page hero-card">
      <div className="split" style={{ alignItems: "center" }}>
        <div>
          <p className="eyebrow">FoodOrder Platform</p>
          <h1>Giao diện đặt món hiện đại, rõ ràng và đầy đủ chức năng</h1>
          <p className="lead">
            Khám phá quán đang sale, món bán chạy và nhanh chóng chuyển đến trang chi tiết chỉ với một cú click.
          </p>
          <div className="row">
            <Link to="/restaurants"><button>Khám phá quán</button></Link>
            <Link to="/foods"><button>Xem món ăn</button></Link>
          </div>
        </div>
        <div className="panel">
          <div className="stat-grid">
            <div className="stat-card"><span>User</span><strong>Đặt món</strong></div>
            <div className="stat-card"><span>Seller</span><strong>Quản lý</strong></div>
            <div className="stat-card"><span>Admin</span><strong>Điều phối</strong></div>
          </div>
          <div style={{ marginTop: 16 }}>
            <span className="badge">Vai trò hiện tại</span>
            <p style={{ marginBottom: 0 }}><b>{roleLabel}</b></p>
          </div>
        </div>
      </div>

      <div className="page" style={{ marginTop: 20 }}>
        <div className="page-header">
          <div>
            <p className="eyebrow">Sale nổi bật</p>
            <h2>Quán và món đang sale</h2>
          </div>
        </div>
        {loading ? (
          <SkeletonCardGrid count={4} />
        ) : (
          <div className="cards">
            {saleRestaurants.slice(0, 2).map((r) => (
              <article key={`sr-${r.id}`} className="panel">
                {r.coverImage && <img src={resolveImageUrl(r.coverImage)} alt={r.name} style={{ width: "100%", height: 180, objectFit: "cover", borderRadius: 16, marginBottom: 12 }} />}
                <div className="row" style={{ justifyContent: "space-between" }}>
                  <span className="badge">-{r.salePercent}%</span>
                  <span className="muted">Quán</span>
                </div>
                <h3>{r.name}</h3>
                <p className="muted">{r.address}</p>
                <div className="card-actions">
                  <div className="left">
                    <Link to={`/restaurants/${r.id}`} className="link-btn">Xem quán</Link>
                  </div>
                </div>
              </article>
            ))}
            {saleFoods.slice(0, 2).map((f) => (
              <article key={`sf-${f.id}`} className="panel">
                {f.image && <img src={resolveImageUrl(f.image)} alt={f.name} style={{ width: "100%", height: 180, objectFit: "cover", borderRadius: 16, marginBottom: 12 }} />}
                <div className="row" style={{ justifyContent: "space-between" }}>
                  <span className="badge">-{f.salePercent}%</span>
                  <span className="muted">Món</span>
                </div>
                <h3>{f.name}</h3>
                <p className="muted">{f.restaurantName}</p>
                <div className="card-actions">
                  <div className="left">
                    <Link to={`/foods/${f.id}`} className="link-btn">Xem món</Link>
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>

      <div className="page" style={{ marginTop: 20 }}>
        <div className="page-header">
          <div>
            <p className="eyebrow">Bán chạy</p>
            <h2>Món có lượt mua cao</h2>
          </div>
        </div>
        {loading ? <SkeletonCardGrid count={6} /> : (
          <div className="cards">
            {bestFoods.map((f) => (
              <article key={`bf-${f.id}`} className="panel">
                {f.image && <img src={resolveImageUrl(f.image)} alt={f.name} style={{ width: "100%", height: 180, objectFit: "cover", borderRadius: 16, marginBottom: 12 }} />}
                <span className="badge">Bán chạy</span>
                <h3>{f.name}</h3>
                <p className="muted">{f.restaurantName}</p>
                <div className="card-actions">
                  <Link to={`/foods/${f.id}`} className="link-btn">Xem chi tiết</Link>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}
