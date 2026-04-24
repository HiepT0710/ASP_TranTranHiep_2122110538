import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getRestaurants, resolveImageUrl } from "../services/apiService";
import { SkeletonCardGrid, StateMessage } from "../components/PageStates";

export default function RestaurantsPage() {
  const [q, setQ] = useState("");
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const loadData = async () => {
    setLoading(true);
    setError("");
    try {
      const data = await getRestaurants({ page: 1, pageSize: 20, q });
      setItems(data.items || []);
    } catch (e) {
      setError(e?.response?.data?.message || "Không tải được danh sách quán");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  return (
    <section className="page">
      <div className="page-header">
        <div>
          <p className="eyebrow">Danh sách quán</p>
          <h2>Khám phá nhà hàng và quán ăn</h2>
          <p className="muted">Chọn quán, xem gallery ảnh, sale và truy cập menu ngay.</p>
        </div>
        <div className="page-actions">
          <div className="search-box">
            <input placeholder="Tìm quán..." value={q} onChange={(e) => setQ(e.target.value)} />
            <button onClick={loadData}>Tìm</button>
          </div>
        </div>
      </div>
      {error ? (
        <StateMessage title="Không tải được quán" description={error} action={<button onClick={loadData}>Thử lại</button>} />
      ) : loading ? (
        <SkeletonCardGrid count={6} />
      ) : items.length === 0 ? (
        <StateMessage title="Không tìm thấy quán" description="Thử thay đổi từ khóa hoặc quay lại sau khi quán được duyệt." />
      ) : (
        <div className="cards">
          {items.map((r) => (
            <article key={r.id} className="panel">
              {(r.coverImage || r.galleryImage1) ? (
                <img src={resolveImageUrl(r.coverImage || r.galleryImage1)} alt={r.name} style={{ width: "100%", height: 180, objectFit: "cover", borderRadius: 16, marginBottom: 12 }} />
              ) : (
                <div className="soft-panel" style={{ width: "100%", height: 180, borderRadius: 16, marginBottom: 12, display: "grid", placeItems: "center" }}>
                  <span className="muted">Chưa có ảnh</span>
                </div>
              )}
              <div className="row" style={{ justifyContent: "space-between" }}>
                <span className="badge">{r.foodCount ?? 0} món</span>
                {r.isOnSale && <span className="badge">Sale -{r.salePercent}%</span>}
              </div>
              <h3>{r.name}</h3>
              <p className="muted">{r.address}</p>
              <p>{r.phone}</p>
              <div className="card-actions">
                <div className="left">
                  <Link to={`/restaurants/${r.id}`} className="link-btn">Xem chi tiết</Link>
                  <Link to={`/foods?restaurantId=${r.id}`} className="link-btn">Xem món</Link>
                </div>
                <div className="right">
                  <button className="secondary" onClick={() => window.location.assign(`/restaurants/${r.id}`)}>Mở quán</button>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
