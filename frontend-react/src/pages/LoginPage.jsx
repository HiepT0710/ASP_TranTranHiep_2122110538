import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { getApiErrorMessage } from "../utils/errorMessage";
import { validateRequired } from "../utils/formValidation";
import { useToast } from "../context/ToastContext";

export default function LoginPage() {
  const { login } = useAuth();
  const { pushToast } = useToast();
  const navigate = useNavigate();
  const [form, setForm] = useState({ username: "", password: "" });
  const [msg, setMsg] = useState("");

  const submit = async (e) => {
    e.preventDefault();
    setMsg("");
    const missing = validateRequired([
      { key: "username", value: form.username },
      { key: "password", value: form.password },
    ], { username: "Username", password: "Mật khẩu" });
    if (missing) {
      const message = `Vui lòng nhập ${missing}`;
      setMsg(message);
      pushToast(message, "error");
      return;
    }
    try {
      await login(form);
      pushToast("Đăng nhập thành công", "success");
      navigate("/");
    } catch (error) {
      const message = getApiErrorMessage(error, "Đăng nhập thất bại");
      setMsg(message);
      pushToast(message, "error");
    }
  };

  return (
    <section className="page center-page">
      <div className="hero-card center-card panel center-hero">
      <p className="eyebrow">Chào mừng quay lại</p>
      <h2>Đăng nhập tài khoản</h2>
      <p className="muted">Đăng nhập để quản lý đơn hàng, hồ sơ và các khu vực seller/admin nếu có quyền.</p>
      <form className="form" onSubmit={submit}>
        <input placeholder="Username" minLength={3} value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} />
        <input type="password" placeholder="Password" minLength={8} value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
        <button type="submit">Đăng nhập</button>
      </form>
      <div className="row" style={{ justifyContent: "space-between" }}>
        <span className="muted">Chưa có tài khoản?</span>
        <div className="row">
          <Link to="/forgot-password">Quên mật khẩu?</Link>
          <Link to="/register">Đăng ký ngay</Link>
        </div>
      </div>
      {msg && <p className="error">{msg}</p>}
      </div>
    </section>
  );
}
