import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { addToCart, getFoods, getFoodCategories, getFoodCategoriesByRouteId, resolveImageUrl } from "../services/apiService";
import { SkeletonCardGrid, StateMessage } from "../components/PageStates";
import { useToast } from "../context/ToastContext";
import SortFilterBar from "../components/SortFilterBar";

export default function FoodsPage() {
  const { pushToast } = useToast();
  const [params] = useSearchParams();
  const [q, setQ] = useState(params.get("q") || "");
  const [foods, setFoods] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [msg, setMsg] = useState("");
  const [filter, setFilter] = useState({ page: 1, pageSize: 20, categoryId: "", sortBy: "name_asc" });
  const [selectedCategoryName, setSelectedCategoryName] = useState("");
  const [meta, setMeta] = useState({ page: 1, total: 0, totalPages: 1 });
  const loadData = async (nextFilter = filter) => {
    setLoading(true);
    setError("");
    try {
      const restaurantId = params.get("restaurantId") || undefined;
      const foodPromise = getFoods({ ...nextFilter, q, restaurantId });
      const categorySourcePromise = restaurantId
        ? getFoodCategories(restaurantId).catch(() => getFoodCategoriesByRouteId(restaurantId).catch(() => null))
        : getFoods({ q, restaurantId, page: 1, pageSize: 50, sortBy: "name_asc" }).catch(() => ({ items: [] }));
      const [data, cats] = await Promise.all([foodPromise, categorySourcePromise]);
      const categoryMap = new Map();
      if (restaurantId) {
        (cats?.items || []).forEach((c) => {
          if (c?.id != null && !categoryMap.has(String(c.id))) categoryMap.set(String(c.id), { id: c.id, name: c.name });
        });
      } else {
        (data.items || []).forEach((f) => {
          if (f?.categoryId != null && !categoryMap.has(String(f.categoryId)) && f.categoryName) {
            categoryMap.set(String(f.categoryId), { id: f.categoryId, name: f.categoryName });
          }
        });
      }
      setCategories(Array.from(categoryMap.values()).slice(0, 8));
      const total = data.total || 0;
      const totalPages = Math.max(1, Math.ceil(total / nextFilter.pageSize));
      const page = Math.min(data.page || nextFilter.page, totalPages);
      setFoods(data.items || []);
      setMeta({ page, total, totalPages });
      setFilter((current) => (current.page > totalPages ? { ...current, page: totalPages } : current));
    } catch (e) {
      setError(e?.response?.data?.message || "Không tải được danh sách món");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData(filter);
  }, [filter.page, filter.pageSize, filter.categoryId, filter.sortBy, q, params]);

  useEffect(() => {
    if (filter.categoryId && categories.length > 0) {
      const matched = categories.find((c) => String(c.id) === String(filter.categoryId));
      setSelectedCategoryName(matched?.name || "");
    }
  }, [categories, filter.categoryId]);

  const handleAdd = async (foodId) => {
    try {
      const res = await addToCart({ foodId, quantity: 1 });
      const message = res.message || "Đã thêm vào giỏ hàng";
      setMsg(message);
      pushToast(message, "success");
    } catch (error) {
      const message = error?.response?.data?.message || "Không thêm được";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  return (
    <section className="page">
      <div className="page-header" style={{ alignItems: "center" }}>
        <div>
          <h1 style={{ fontSize: 42, lineHeight: 1.05, marginBottom: 0 }}>Danh sách món ăn</h1>
        </div>
        <div className="page-actions">
          <div className="search-box" style={{ minWidth: 320 }}>
            <input placeholder="Tìm món..." value={q} onChange={(e) => setQ(e.target.value)} />
            <button onClick={() => { pushToast(q ? `Đang tìm kiếm: ${q}` : "Đang tải danh sách món", "info"); loadData({ ...filter, page: 1 }); }}>Tìm</button>
          </div>
        </div>
      </div>
      <SortFilterBar
        className="food-page-filter-bar"
        filters={[
          {
            key: "categoryId",
            type: "select",
            minWidth: 240,
            label: "Danh mục",
            options: [
              { value: "", label: "Tất cả danh mục" },
              ...categories.slice(0, 8).map((c) => ({ value: String(c.id), label: c.name })),
            ],
          },
        ]}
        sortOptions={[
          { value: "name_asc", label: "A → Z" },
          { value: "name_desc", label: "Z → A" },
          { value: "price_asc", label: "Giá ↑" },
          { value: "price_desc", label: "Giá ↓" },
          { value: "rating_desc", label: "Sao cao" },
        ]}
        value={filter}
        onChange={(next) => {
          const nextCategoryId = next.categoryId ?? "";
          setFilter((prev) => ({ ...prev, ...next, page: 1, categoryId: nextCategoryId }));
          setSelectedCategoryName(categories.find((c) => String(c.id) === String(nextCategoryId))?.name || "");
          if (nextCategoryId === "") pushToast("Đã bỏ lọc danh mục", "info");
        }}
      />
      {selectedCategoryName && <div className="badge" style={{ marginBottom: 12 }}>Đang lọc: {selectedCategoryName}</div>}
      {msg && <p className="ok">{msg}</p>}
      {error ? (
        <StateMessage title="Không tải được món" description={error} action={<button onClick={loadData}>Thử lại</button>} />
      ) : loading ? (
        <SkeletonCardGrid count={6} />
      ) : foods.length === 0 ? (
        <StateMessage title="Chưa có món phù hợp" description="Hãy thử từ khóa khác." />
      ) : (
        <>
          <div className="cards">
            {foods.map((f) => (
              <article key={f.id} className="panel">
              {f.image ? (
                <img src={resolveImageUrl(f.image)} alt={f.name} style={{ width: "100%", height: 180, objectFit: "cover", borderRadius: 16, marginBottom: 12 }} />
              ) : (
                <div className="soft-panel" style={{ width: "100%", height: 180, borderRadius: 16, marginBottom: 12, display: "grid", placeItems: "center" }}>
                  <span className="muted">Chưa có ảnh</span>
                </div>
              )}
              <div className="row" style={{ justifyContent: "space-between" }}>
                <span className="badge">{f.categoryName || "Món mới"}</span>
                <span className="badge">{f.avgRating ? `${Number(f.avgRating).toFixed(1)}★` : "Chưa có đánh giá"}</span>
                {f.isOnSale && <span className="badge">Sale -{f.salePercent}%</span>}
              </div>
              <h3>{f.name}</h3>
              <p className="muted">{f.restaurantName}</p>
              <p>Giá: <b>{f.price}</b></p>
              <p className="muted">Tình trạng: <b>{f.stockQuantity > 0 ? "Còn hàng" : "Hết hàng"}</b></p>
              <p className="muted">{f.description || "Chưa có mô tả"}</p>
              <div className="card-actions">
                <div className="left">
                  <Link to={`/foods/${f.id}`} className="button secondary">Xem chi tiết</Link>
                </div>
                <div className="right">
                  <button className="secondary" onClick={() => handleAdd(f.id)}>Thêm giỏ</button>
                </div>
              </div>
            </article>
          ))}
          </div>
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
