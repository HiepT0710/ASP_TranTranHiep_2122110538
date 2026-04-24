import { useEffect, useState } from "react";
import ActionMenu from "../../components/ActionMenu";
import { adminRestaurantAction, getAdminRestaurants } from "../../services/apiService";
import { useToast } from "../../context/ToastContext";

export default function AdminRestaurantsPage() {
  const { pushToast } = useToast();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState("");

  const loadData = async () => {
    setLoading(true);
    try {
      const data = await getAdminRestaurants({ page: 1, pageSize: 50 });
      setItems(data.items || []);
    } catch (error) {
      setMsg(error?.response?.data?.message || "Không tải được danh sách quán");
    } finally {
      setLoading(false);
    }
  };
  useEffect(() => {
    loadData();
  }, []);

  const action = async (id, name) => {
    try {
      await adminRestaurantAction(id, name);
      const message = name === "Approve" ? "Đã duyệt quán" : name === "Reject" ? "Đã từ chối quán" : "Đã tạm ngưng quán";
      setMsg(message);
      pushToast(message, "success");
      loadData();
    } catch (error) {
      const message = error?.response?.data?.message || "Không thể cập nhật quán";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  return (
    <section className="page">
      <div className="row" style={{ justifyContent: "space-between" }}>
        <div>
          <p className="eyebrow">Quản trị quán</p>
          <h2>Admin - Duyệt quán</h2>
        </div>
      </div>
      {msg && <p className="ok">{msg}</p>}
      {loading ? (
        <div className="panel">Đang tải quán...</div>
      ) : (
        <table className="table">
          <thead><tr><th>ID</th><th>Tên quán</th><th>Chủ quán</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
          <tbody>
            {items.map((r) => (
              <tr key={r.id}>
                <td>{r.id}</td>
                <td>{r.name}</td>
                <td>{r.ownerUsername}</td>
                <td><span className="badge">{r.status}</span></td>
                <td>
                  <ActionMenu
                    label="Thao tác"
                    items={[
                      { label: "Duyệt quán", onClick: () => action(r.id, "Approve") },
                      { label: "Từ chối", onClick: () => action(r.id, "Reject") },
                      { label: "Tạm ngưng", onClick: () => action(r.id, "Suspend"), variant: "ghost" },
                    ]}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
