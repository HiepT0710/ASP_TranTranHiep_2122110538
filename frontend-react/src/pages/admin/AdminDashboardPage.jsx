import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getAdminSummary } from "../../services/apiService";

const quickLinks = [
  { to: "/admin/users", label: "Quản lý users" },
  { to: "/admin/restaurants", label: "Duyệt quán" },
  { to: "/admin/foods", label: "Quản lý món ăn" },
  { to: "/admin/orders", label: "Quản lý đơn" },
  { to: "/admin/promotions", label: "Khuyến mãi & Voucher" },
];

export default function AdminDashboardPage() {
  const [data, setData] = useState(null);
  useEffect(() => {
    getAdminSummary().then(setData).catch(() => setData(null));
  }, []);

  return (
    <section className="page hero-card">
      <div className="dashboard-hero">
        <div>
          <p className="eyebrow">Admin control center</p>
          <h2>Bảng điều khiển quản trị</h2>
          <p className="muted">Theo dõi tổng quan hệ thống, thao tác nhanh với users, quán, món và đơn hàng.</p>
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

      <div className="dashboard-summary-grid">
        <article className="dashboard-summary-card"><span>Users</span><strong>{data?.users ?? 0}</strong></article>
        <article className="dashboard-summary-card"><span>Sellers</span><strong>{data?.sellers ?? 0}</strong></article>
        <article className="dashboard-summary-card"><span>Restaurants</span><strong>{data?.restaurants ?? 0}</strong></article>
        <article className="dashboard-summary-card"><span>Orders</span><strong>{data?.orders ?? 0}</strong></article>
      </div>
    </section>
  );
}
