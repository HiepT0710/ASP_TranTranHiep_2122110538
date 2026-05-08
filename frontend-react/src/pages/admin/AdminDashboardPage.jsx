import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getAdminSummary } from "../../services/apiService";

const quickLinks = [
  { to: "/admin/users", label: "Người dùng" },
  { to: "/admin/restaurants", label: "Quán ăn" },
  { to: "/admin/reports", label: "Báo cáo" },
  { to: "/admin/audit", label: "Audit log" },
  { to: "/admin/settings", label: "Cấu hình" },
];

export default function AdminDashboardPage() {
  const [data, setData] = useState(null);
  useEffect(() => {
    getAdminSummary().then(setData).catch(() => setData(null));
  }, []);

  const cards = [
    { label: "Users", value: data?.users ?? 0 },
    { label: "Restaurants", value: data?.restaurants ?? 0 },
    { label: "Orders", value: data?.orders ?? 0 },
    { label: "Revenue", value: `${Number(data?.revenueCompletedOrders || 0).toLocaleString()} đ` },
    { label: "Reports", value: data?.reports ?? 0 },
    { label: "Audit logs", value: data?.auditLogs ?? 0 },
  ];

  return (
    <section className="page hero-card">
      <div className="dashboard-hero">
        <div>
          <p className="eyebrow">Admin control center</p>
          <h2>Bảng điều khiển quản trị</h2>
          <p className="muted">Tổng hợp số liệu quan trọng và truy cập nhanh các tác vụ quản trị chính.</p>
        </div>
        <div className="panel soft-panel" style={{ padding: 18 }}>
          <div className="quick-actions-grid">
            {quickLinks.map((item) => (
              <Link key={item.to} to={item.to} className="quick-action-card">{item.label}</Link>
            ))}
          </div>
        </div>
      </div>

      <div className="dashboard-summary-grid">
        {cards.map((card) => (
          <article key={card.label} className="dashboard-summary-card"><span>{card.label}</span><strong>{card.value}</strong></article>
        ))}
      </div>

    </section>
  );
}
