import { useEffect, useState } from "react";
import { getSellerAuditLogs } from "../../services/apiService";
import { formatDateTime } from "../../utils/dateTime";

export default function SellerAuditHistoryPage() {
  const [data, setData] = useState({ items: [], total: 0, page: 1, pageSize: 20 });
  const [q, setQ] = useState("");

  const load = async (page = 1) => {
    const res = await getSellerAuditLogs({ page, pageSize: data.pageSize, q });
    setData(res);
  };

  useEffect(() => {
    load(1).catch(() => setData({ items: [], total: 0, page: 1, pageSize: 20 }));
  }, []);

  return (
    <section className="page">
      <p className="eyebrow">Seller audit</p>
      <h2>Lịch sử thao tác Seller</h2>
      <div className="row" style={{ marginBottom: 12 }}>
        <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Tìm action / note / actor" />
        <button onClick={() => load(1)}>Lọc</button>
      </div>
      <div className="panel">
        <table className="table">
          <thead><tr><th>ID</th><th>Action</th><th>Actor</th><th>Entity</th><th>Ghi chú</th><th>Thời gian</th></tr></thead>
          <tbody>
            {data.items.map((x) => (
              <tr key={x.id}>
                <td>{x.id}</td>
                <td>{x.action}</td>
                <td>{x.actor}</td>
                <td>{x.entityType} {x.entityId ? `#${x.entityId}` : ""}</td>
                <td>{x.note}</td>
                <td>{formatDateTime(x.createdAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
