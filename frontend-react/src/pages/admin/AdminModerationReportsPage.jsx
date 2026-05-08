import { useEffect, useState } from "react";
import { getAdminReports, resolveAdminReport } from "../../services/apiService";
import { formatDateTime } from "../../utils/dateTime";
import { useToast } from "../../context/ToastContext";

export default function AdminModerationReportsPage() {
  const { pushToast } = useToast();
  const [items, setItems] = useState([]);

  const loadData = async () => {
    try {
      const data = await getAdminReports();
      setItems(data.items || []);
    } catch {
      setItems([]);
    }
  };

  useEffect(() => { loadData(); }, []);

  const resolve = async (id) => {
    try {
      await resolveAdminReport(id, { status: "Resolved" });
      pushToast("Đã xử lý report", "success");
      loadData();
    } catch (error) {
      pushToast(error?.response?.data?.message || "Không xử lý được report", "error");
    }
  };

  return (
    <section className="page">
      <p className="eyebrow">Báo cáo / kiểm duyệt</p>
      <h2>Quản lý báo cáo</h2>
      <div className="panel">
        <table className="table">
          <thead><tr><th>ID</th><th>Người báo</th><th>Đối tượng</th><th>Lý do</th><th>Trạng thái</th><th>Thời gian</th><th>Thao tác</th></tr></thead>
          <tbody>
            {items.map((x) => (
              <tr key={x.id}>
                <td>{x.id}</td>
                <td>{x.reporter?.username || x.reporterUsername || "N/A"}</td>
                <td>{x.targetType} #{x.targetId}</td>
                <td>{x.reason}</td>
                <td><span className="badge">{x.status}</span></td>
                <td>{formatDateTime(x.createdAt)}</td>
                <td><button onClick={() => resolve(x.id)}>Đánh dấu đã xử lý</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
