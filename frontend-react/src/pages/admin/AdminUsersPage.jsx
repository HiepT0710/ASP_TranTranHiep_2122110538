import { useEffect, useState } from "react";
import ActionMenu from "../../components/ActionMenu";
import {
  getAdminUserDetails,
  getAdminUsers,
  lockAdminUser,
  resetAdminUserRole,
  unlockAdminUser,
  updateAdminUserRole,
} from "../../services/apiService";
import { useToast } from "../../context/ToastContext";
import { formatDateTime } from "../../utils/dateTime";

const ROLE_LABELS = {
  User: "Người dùng",
  Seller: "Người bán",
  Admin: "Quản trị",
};

export default function AdminUsersPage() {
  const { pushToast } = useToast();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState("");
  const [detail, setDetail] = useState(null);

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

  const loadDetail = async (id) => {
    try {
      const data = await getAdminUserDetails(id);
      setDetail(data);
    } catch (error) {
      setDetail(null);
      setMsg(error?.response?.data?.message || "Không tải được chi tiết user");
    }
  };
  useEffect(() => {
    loadData();
  }, []);

  const runAction = async (handler, successMessage) => {
    try {
      await handler();
      setMsg(successMessage);
      pushToast(successMessage, "success");
      loadData();
    } catch (error) {
      const message = error?.response?.data?.message || "Thao tác thất bại";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  const setRole = async (id, role) => runAction(() => updateAdminUserRole(id, role), `Đã cập nhật vai trò sang ${ROLE_LABELS[role] || role}`);
  const resetRole = async (id) => runAction(() => resetAdminUserRole(id), "Đã reset vai trò");
  const lockUser = async (id) => runAction(() => lockAdminUser(id, "Khóa bởi Admin"), "Đã khóa tài khoản");
  const unlockUser = async (id) => runAction(() => unlockAdminUser(id), "Đã mở khóa tài khoản");

  return (
    <section className="page">
      <div className="row" style={{ justifyContent: "space-between", flexWrap: "wrap" }}>
        <div>
          <p className="eyebrow">Quản trị người dùng</p>
          <h2>Admin - Quản lý người dùng</h2>
        </div>
      </div>
      {msg && <p className="ok">{msg}</p>}
      {loading ? (
        <div className="panel">Đang tải danh sách người dùng...</div>
      ) : (
        <>
          <table className="table">
            <thead><tr><th>ID</th><th>Tên đăng nhập</th><th>Vai trò</th><th>Trạng thái</th><th>Thời gian tạo</th><th>Thao tác</th></tr></thead>
            <tbody>
              {items.map((u) => (
                <tr key={u.id}>
                  <td>{u.id}</td>
                  <td>{u.username}</td>
                  <td><span className="badge">{ROLE_LABELS[u.role] || u.role}</span></td>
                  <td>{u.isLocked ? <span className="badge">Đã khóa</span> : <span className="badge">Hoạt động</span>}</td>
                  <td>{formatDateTime(u.createdAt)}</td>
                  <td>
                    <ActionMenu
                      label="Thao tác"
                      items={[
                        { label: "Xem chi tiết", onClick: () => loadDetail(u.id) },
                        { label: ROLE_LABELS.User, onClick: () => setRole(u.id, "User") },
                        { label: ROLE_LABELS.Seller, onClick: () => setRole(u.id, "Seller") },
                        { label: "Reset vai trò", onClick: () => resetRole(u.id) },
                        { label: u.isLocked ? "Mở khóa" : "Khóa tài khoản", onClick: () => (u.isLocked ? unlockUser(u.id) : lockUser(u.id)), variant: "ghost" },
                      ]}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {detail && (
            <div className="panel" style={{ marginTop: 16 }}>
              <h3>Chi tiết user #{detail.id}</h3>
              <p className="muted">Username: <b>{detail.username}</b></p>
              <p className="muted">Họ tên: <b>{detail.fullName || "N/A"}</b></p>
              <p className="muted">Email: <b>{detail.email || "N/A"}</b></p>
              <p className="muted">SĐT: <b>{detail.phone || "N/A"}</b></p>
              <p className="muted">Địa chỉ: <b>{detail.address || "N/A"}</b></p>
              <p className="muted">Vai trò: <b>{ROLE_LABELS[detail.role] || detail.role}</b></p>
              <p className="muted">Trạng thái: <b>{detail.isLocked ? "Đã khóa" : "Hoạt động"}</b></p>
              <p className="muted">Lý do khóa: <b>{detail.lockReason || "N/A"}</b></p>
              <p className="muted">Tạo lúc: <b>{formatDateTime(detail.createdAt)}</b></p>
              <h4 style={{ marginTop: 16 }}>Lịch sử hoạt động</h4>
              {(detail.history || []).length === 0 ? (
                <p className="muted">Chưa có lịch sử.</p>
              ) : (
                <ul>
                  {(detail.history || []).map((h) => (
                    <li key={h.id}>{h.action} - {h.note || ""} - {formatDateTime(h.createdAt)}</li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </>
      )}
    </section>
  );
}
