import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { getSellerDashboard, getSellerSummary } from "../../services/apiService";

const quickLinks = [
  { to: "/seller/categories", label: "Quản lý danh mục" },
  { to: "/seller/foods", label: "Quản lý món" },
  { to: "/seller/foods/new", label: "Thêm món mới" },
  { to: "/seller/orders", label: "Quản lý đơn" },
  { to: "/seller/promotions", label: "Khuyến mãi & Voucher" },
  { to: "/seller/restaurant", label: "Ảnh quán của tôi" },
  { to: "/seller/operations", label: "Vận hành quán" },
  { to: "/seller/audit", label: "Lịch sử thao tác" },
];

export default function SellerDashboardPage() {
  const [data, setData] = useState(null);
  const [ops, setOps] = useState(null);
  const [range, setRange] = useState({ bucket: "day", from: "", to: "" });

  const loadDashboard = async () => {
    const params = { bucket: range.bucket };
    if (range.from) params.from = range.from;
    if (range.to) params.to = range.to;
    const [summary, dashboard] = await Promise.all([
      getSellerSummary(),
      getSellerDashboard(params),
    ]);
    setData(summary);
    setOps(dashboard);
  };

  useEffect(() => {
    loadDashboard().catch(() => {
      setData(null);
      setOps(null);
    });
  }, [range.bucket, range.from, range.to]);

  const revenueSeries = useMemo(() => ops?.revenueSeries || [], [ops]);
  const maxRevenue = useMemo(() => Math.max(1, ...revenueSeries.map((x) => Number(x.total || 0))), [revenueSeries]);

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

      <div className="row" style={{ marginBottom: 16, flexWrap: "wrap" }}>
        <select value={range.bucket} onChange={(e) => setRange((cur) => ({ ...cur, bucket: e.target.value }))}>
          <option value="day">Theo ngày</option>
          <option value="month">Theo tháng</option>
        </select>
        <input type="date" value={range.from} onChange={(e) => setRange((cur) => ({ ...cur, from: e.target.value }))} />
        <input type="date" value={range.to} onChange={(e) => setRange((cur) => ({ ...cur, to: e.target.value }))} />
      </div>

      <div className="dashboard-summary-grid three">
        <article className="dashboard-summary-card"><span>Tổng đơn</span><strong>{data?.ordersTotal ?? 0}</strong></article>
        <article className="dashboard-summary-card"><span>Đơn hôm nay</span><strong>{data?.ordersToday ?? 0}</strong></article>
        <article className="dashboard-summary-card"><span>Doanh thu</span><strong>{data?.revenueCompletedExcludingRefunds ?? 0}</strong></article>
      </div>

      <div className="dashboard-summary-grid three" style={{ marginTop: 16 }}>
        <article className="dashboard-summary-card"><span>Đơn hoàn thành</span><strong>{ops?.successCount ?? 0}</strong></article>
        <article className="dashboard-summary-card"><span>Đơn hủy</span><strong>{ops?.cancelledCount ?? 0}</strong></article>
        <article className="dashboard-summary-card"><span>Tỷ lệ chuyển đổi</span><strong>{Math.round((ops?.conversionRate ?? 0) * 100)}%</strong></article>
      </div>

      <div className="dashboard-grid-two" style={{ marginTop: 16 }}>
        <div className="panel soft-panel dashboard-chart-panel">
          <div className="panel-heading">
            <div>
              <h3>Biểu đồ doanh thu</h3>
              <p className="muted">Doanh thu theo mốc thời gian đã chọn</p>
            </div>
          </div>
          {revenueSeries.length > 0 ? (
            <div className="chart-bars">
              {revenueSeries.map((item) => {
                const width = `${Math.max(6, Math.round((Number(item.total || 0) / maxRevenue) * 100))}%`;
                return (
                  <div key={item.label} className="chart-bar-row">
                    <span className="chart-label">{item.label}</span>
                    <div className="chart-bar-track"><div className="chart-bar-fill" style={{ width }} /></div>
                    <strong className="chart-value">{Number(item.total || 0).toLocaleString()} đ</strong>
                  </div>
                );
              })}
            </div>
          ) : (
            <div className="empty-state">Chưa có dữ liệu doanh thu cho khoảng thời gian này.</div>
          )}
        </div>

        <div className="panel soft-panel dashboard-chart-panel">
          <div className="panel-heading">
            <div>
              <h3>Món bán chạy</h3>
              <p className="muted">Top món có số lượng bán cao nhất</p>
            </div>
          </div>
          {ops?.bestSellers?.length > 0 ? (
            <div className="mini-stat-list">
              {ops.bestSellers.map((item) => (
                <div key={item.foodId} className="mini-stat-item">
                  <span>{item.foodName}</span>
                  <strong>{item.quantity}</strong>
                </div>
              ))}
            </div>
          ) : (
            <div className="empty-state">Chưa có dữ liệu món bán chạy.</div>
          )}
        </div>
      </div>
    </section>
  );
}
