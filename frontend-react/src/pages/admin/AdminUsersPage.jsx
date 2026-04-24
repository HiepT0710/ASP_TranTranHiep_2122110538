import { useEffect, useState } from "react";
import ActionMenu from "../../components/ActionMenu";
import { getAdminUsers, updateAdminUserRole } from "../../services/apiService";
import { useToast } from "../../context/ToastContext";

export default function AdminUsersPage() {
  const { pushToast } = useToast();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState("");

  const loadData = async () => {
    setLoading(true);
    try {
      const data = await getAdminUsers({ page: 1, pageSize: 50 });
      setItems(data.items || []);
    } catch (error) {
      setMsg(error?.response?.data?.message || "Không tải được danh sách user");
    } finally {
      setLoading(false);
    }
  };
  useEffect(() => {
    loadData();
  }, []);

  const setRole = async (id, role) => {
    try {
      await updateAdminUserRole(id, role);
      const message = `Đã cập nhật role sang ${role}`;
      setMsg(message);
      pushToast(message, "success");
      loadData();
    } catch (error) {
      const message = error?.response?.data?.message || "Không cập nhật được role";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  return (
    <section className="page">
      <div className="row" style={{ justifyContent: "space-between" }}>
        <div>
          <p className="eyebrow">Quản trị user</p>
          <h2>Admin - Quản lý users</h2>
        </div>
      </div>
      {msg && <p className="ok">{msg}</p>}
      {loading ? (
        <div className="panel">Đang tải danh sách users...</div>
      ) : (
        <table className="table">
          <thead><tr><th>ID</th><th>Username</th><th>Role</th><th>Đổi role</th></tr></thead>
          <tbody>
            {items.map((u) => (
              <tr key={u.id}>
                <td>{u.id}</td>
                <td>{u.username}</td>
                <td><span className="badge">{u.role}</span></td>
                <td>
                  <ActionMenu
                    label="Đổi role"
                    items={[
                      { label: "User", onClick: () => setRole(u.id, "User") },
                      { label: "Seller", onClick: () => setRole(u.id, "Seller") },
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
