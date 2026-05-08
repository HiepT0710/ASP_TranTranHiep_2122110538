import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import SortFilterBar from "../components/SortFilterBar";
import { getBestSellers, getRestaurantSale, getRestaurants, resolveImageUrl } from "../services/apiService";
import { SkeletonCardGrid, StateMessage } from "../components/PageStates";
import { useToast } from "../context/ToastContext";

export default function RestaurantsPage() {
  useToast();
  const [restaurants, setRestaurants] = useState([]);
  const [highlights, setHighlights] = useState({ sale: [], best: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [filter, setFilter] = useState({ page: 1, pageSize: 12, q: "", sortBy: "rating_desc" });
  const [meta, setMeta] = useState({ page: 1, total: 0, totalPages: 1 });

  const loadData = async () => {
    setLoading(true);
    setError("");
    try {
      const [data, sale, best] = await Promise.all([
        getRestaurants(filter),
        getRestaurantSale().catch(() => ({ items: [] })),
        getBestSellers(6).catch(() => ({ items: [] })),
      ]);
      const total = data.total || 0;
      const totalPages = Math.max(1, Math.ceil(total / filter.pageSize));
      setRestaurants(data.items || []);
      setMeta({ page: Math.min(data.page || filter.page, totalPages), total, totalPages });
      setHighlights({ sale: sale.items || [], best: best.items || [] });
    } catch (e) {
      setError(e?.response?.data?.message || "Không tải được danh sách quán");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadData(); }, [filter.page, filter.pageSize, filter.q, filter.sortBy]);

  return (
    <section className="page">
      <div className="page-header" style={{ alignItems: "center" }}>
        <div>
          <p className="eyebrow">Khám phá quán</p>
          <h1>Quán ăn nổi bật</h1>
        </div>
      </div>

      <SortFilterBar
        value={filter}
        onChange={setFilter}
        filters={[{ key: "q", label: "Tìm quán", type: "input", placeholder: "Tên quán / địa chỉ" }]}
        sortOptions={[
          { value: "rating_desc", label: "Đánh giá cao" },
          { value: "name_asc", label: "Tên A → Z" },
          { value: "name_desc", label: "Tên Z → A" },
          { value: "newest", label: "Mới nhất" },
          { value: "oldest", label: "Cũ nhất" },
        ]}
      />

      {error ? (
        <StateMessage title="Không tải được quán" description={error} action={<button onClick={loadData}>Thử lại</button>} />
      ) : loading ? (
        <SkeletonCardGrid count={6} />
      ) : (
        <>
          {highlights.sale.length > 0 && (
            <div className="panel" style={{ marginBottom: 16 }}>
              <h3>Quán đang sale</h3>
              <div className="cards">
                {highlights.sale.slice(0, 3).map((r) => (
                  <article key={r.id} className="panel soft-panel">
                    <b>{r.name}</b>
                    <p className="muted">Giảm đến {r.salePercent}%</p>
                  </article>
                ))}
              </div>
            </div>
          )}

          {restaurants.length === 0 ? (
            <StateMessage title="Chưa có quán phù hợp" description="Hãy thử từ khóa khác hoặc đổi sắp xếp." />
          ) : (
            <div className="cards">
              {restaurants.map((r) => (
                <article key={r.id} className="panel">
                  {r.coverImage ? (
                    <img src={resolveImageUrl(r.coverImage)} alt={r.name} style={{ width: "100%", height: 190, objectFit: "cover", borderRadius: 16, marginBottom: 12 }} />
                  ) : (
                    <div className="soft-panel" style={{ width: "100%", height: 190, borderRadius: 16, marginBottom: 12, display: "grid", placeItems: "center" }}>Chưa có ảnh</div>
                  )}
                  <div className="row" style={{ justifyContent: "space-between" }}>
                    <span className="badge">{r.avgRating ? `${Number(r.avgRating).toFixed(1)}★` : "Chưa có đánh giá"}</span>
                    <span className="badge">{r.foodCount || 0} món</span>
                    {r.isOnSale && <span className="badge">Sale -{r.salePercent}%</span>}
                  </div>
                  <h3>{r.name}</h3>
                  <p className="muted">{r.address || "Chưa cập nhật địa chỉ"}</p>
                  <p className="muted">SĐT: {r.phone || "Chưa cập nhật"}</p>
                  <div className="card-actions">
                    <Link className="button secondary" to={`/restaurants/${r.id}`}>Xem chi tiết</Link>
                    <Link className="button" to={`/foods?restaurantId=${r.id}`}>Xem món</Link>
                  </div>
                </article>
              ))}
            </div>
          )}

          {meta.totalPages > 1 && (
            <div className="row">
              <button className="secondary" disabled={filter.page <= 1} onClick={() => setFilter({ ...filter, page: filter.page - 1 })}>Prev</button>
              <span className="badge">Trang {meta.page} / {meta.totalPages}</span>
              <button className="secondary" disabled={filter.page >= meta.totalPages} onClick={() => setFilter({ ...filter, page: filter.page + 1 })}>Next</button>
            </div>
          )}
        </>
      )}
    </section>
  );
}
