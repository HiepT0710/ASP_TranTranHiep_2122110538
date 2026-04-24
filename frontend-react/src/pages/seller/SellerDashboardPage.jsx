import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getSellerSummary } from "../../services/apiService";

const quickLinks = [
  { to: "/seller/categories", label: "Quản lý danh mục" },
  { to: "/seller/foods", label: "Quản lý món" },
  { to: "/seller/foods/new", label: "Thêm món mới" },
  { to: "/seller/orders", label: "Quản lý đơn" },
  { to: "/seller/promotions", label: "Khuyến mãi & Voucher" },
  { to: "/seller/restaurant", label: "Ảnh quán của tôi" },
];

export default function SellerDashboardPage() {
  const [data, setData] = useState(null);
  useEffect(() => {
    getSellerSummary().then(setData).catch(() => setData(null));
  }, []);

  return (
    <section className="page hero-card">
      <div className="dashboard-hero">
        <div>
          <p className="eyebrow">Seller workspace</p>
          <h2>Dashboard của seller</h2>
          <p className="muted">Quản lý hoạt động bán hàng, danh mục, món và đơn theo cách rõ ràng, trực quan.</p>
        </div>
        <div className="panel soft-panel" style={{ padding: 18 }}>
          <p className="eyebrow" style={{ marginBottom: 10 }}>Lối tắt thao tác</p>
          <div className="quick-actions-grid">
            {quickLinks.map((item) => (
              <Link key={item.to} to={item.to} className="quick-action-card">{item.label}</Link>
            ))}
          </div>
        </div>
      </div>

      <div className="dashboard-summary-grid three">
        <article className="dashboard-summary-card"><span>Tổng đơn</span><strong>{data?.ordersTotal ?? 0}</strong></article>
        <article className="dashboard-summary-card"><span>Đơn hôm nay</span><strong>{data?.ordersToday ?? 0}</strong></article>
        <article className="dashboard-summary-card"><span>Doanh thu</span><strong>{data?.revenueCompletedExcludingRefunds ?? 0}</strong></article>
      </div>
    </section>
  );
}
