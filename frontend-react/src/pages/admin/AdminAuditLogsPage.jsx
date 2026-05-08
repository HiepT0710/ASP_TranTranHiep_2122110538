import { useEffect, useState } from "react";
import { getAdminAuditLogs } from "../../services/apiService";
import { formatDateTime } from "../../utils/dateTime";

export default function AdminAuditLogsPage() {
  const [items, setItems] = useState([]);

  useEffect(() => {
    getAdminAuditLogs().then((r) => setItems(r.items || [])).catch(() => setItems([]));
  }, []);

  return (
    <section className="page">
      <p className="eyebrow">Audit log</p>
      <h2>Nhật ký hoạt động hệ thống</h2>
      <div className="panel">
        <table className="table">
          <thead><tr><th>ID</th><th>Hành động</th><th>Người thực hiện</th><th>Đối tượng</th><th>Ghi chú</th><th>Thời gian</th></tr></thead>
          <tbody>
            {items.map((x) => (
              <tr key={x.id}>
                <td>{x.id}</td>
                <td>{x.action}</td>
                <td>{x.actor}</td>
                <td>{x.entityType} #{x.entityId}</td>
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
