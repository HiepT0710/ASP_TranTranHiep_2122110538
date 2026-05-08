import { useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { resetPassword } from "../services/apiService";
import { useToast } from "../context/ToastContext";

export default function ResetPasswordPage() {
  const { pushToast } = useToast();
  const navigate = useNavigate();
  const location = useLocation();
  const search = useMemo(() => new URLSearchParams(location.search), [location.search]);
  const emailFromLink = search.get("email") || "";
  const tokenFromLink = search.get("token") || "";
  const [form, setForm] = useState({
    email: emailFromLink,
    token: tokenFromLink,
    newPassword: "",
    confirmPassword: "",
  });
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState("");

  const submit = async (e) => {
    e.preventDefault();
    if (form.newPassword !== form.confirmPassword) {
      return pushToast("Mật khẩu mới và xác nhận mật khẩu không khớp", "error");
    }
    if (!form.email || !form.token) {
      return pushToast("Vui lòng mở link trong email để tự xác thực tài khoản", "error");
    }
    setLoading(true);
    try {
      const res = await resetPassword(form);
      setMsg(res.message);
      pushToast(res.message, "success");
      setTimeout(() => navigate("/login"), 1200);
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="page center-page">
      <div className="hero-card center-card panel center-hero">
        <p className="eyebrow">Đặt lại mật khẩu</p>
        <h2>Reset password</h2>
        <p className="muted">Nhập mật khẩu mới để hoàn tất. Thông tin xác thực sẽ được lấy từ link trong email.</p>
        <p className="muted" style={{ fontSize: 14 }}>Email: <b>{form.email || "-"}</b></p>
        <form className="form" onSubmit={submit}>
          <input type="email" placeholder="Email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
          <input type="password" placeholder="Mật khẩu mới" value={form.newPassword} onChange={(e) => setForm({ ...form, newPassword: e.target.value })} />
          <input type="password" placeholder="Xác nhận mật khẩu" value={form.confirmPassword} onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })} />
          <button type="submit" disabled={loading}>{loading ? "Đang xử lý..." : "Đặt lại mật khẩu"}</button>
        </form>
        {msg && <p className="ok">{msg}</p>}
      </div>
    </section>
  );
}
