import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { addToCart, getFoods, resolveImageUrl } from "../services/apiService";
import { SkeletonCardGrid, StateMessage } from "../components/PageStates";
import { useToast } from "../context/ToastContext";

export default function FoodsPage() {
  const { pushToast } = useToast();
  const [params] = useSearchParams();
  const [q, setQ] = useState(params.get("q") || "");
  const [foods, setFoods] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [msg, setMsg] = useState("");

  const loadData = async () => {
    setLoading(true);
    setError("");
    try {
      const data = await getFoods({ page: 1, pageSize: 20, q, restaurantId: params.get("restaurantId") || undefined });
      setFoods(data.items || []);
    } catch (e) {
      setError(e?.response?.data?.message || "Không tải được danh sách món");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

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
      <div className="page-header">
        <div>
          <p className="eyebrow">Grid món ăn</p>
          <h2>Chọn món và thêm vào giỏ nhanh chóng</h2>
          <p className="muted">Món đang sale sẽ được gắn badge rõ ràng, còn món bán chạy được ưu tiên hiển thị ở trang chủ.</p>
        </div>
        <div className="page-actions">
          <div className="search-box">
            <input placeholder="Tìm món..." value={q} onChange={(e) => setQ(e.target.value)} />
            <button onClick={loadData}>Tìm</button>
          </div>
        </div>
      </div>
      {msg && <p className="ok">{msg}</p>}
      {error ? (
        <StateMessage title="Không tải được món" description={error} action={<button onClick={loadData}>Thử lại</button>} />
      ) : loading ? (
        <SkeletonCardGrid count={6} />
      ) : foods.length === 0 ? (
        <StateMessage title="Chưa có món phù hợp" description="Hãy thử từ khóa khác hoặc quay về danh sách quán để khám phá." />
      ) : (
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
                {f.isOnSale && <span className="badge">Sale -{f.salePercent}%</span>}
              </div>
              <h3>{f.name}</h3>
              <p className="muted">{f.restaurantName}</p>
              <p>Giá: <b>{f.price}</b></p>
              <p className="muted">{f.description || "Chưa có mô tả"}</p>
              <div className="card-actions">
                <div className="left">
                  <Link to={`/foods/${f.id}`} className="link-btn">Xem chi tiết</Link>
                </div>
                <div className="right">
                  <button className="secondary" onClick={() => handleAdd(f.id)}>Thêm giỏ</button>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
