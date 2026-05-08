import { useState } from "react";
import { forgotPassword } from "../services/apiService";
import { useToast } from "../context/ToastContext";

export default function ForgotPasswordPage() {
  const { pushToast } = useToast();
  const [email, setEmail] = useState("");
  const [msg, setMsg] = useState("");

  const submit = async (e) => {
    e.preventDefault();
    const res = await forgotPassword({ email });
    setMsg(res.message);
    pushToast(res.message, "success");
  };

  return (
    <section className="page center-page">
      <div className="hero-card center-card panel center-hero">
        <p className="eyebrow">Khôi phục tài khoản</p>
        <h2>Quên mật khẩu</h2>
        <p className="muted">Nhập email để nhận hướng dẫn đặt lại mật khẩu qua gmail.</p>
        <form className="form" onSubmit={submit}>
          <input type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} />
          <button type="submit">Gửi email</button>
        </form>
        {msg && <p className="ok">{msg}</p>}
      </div>
    </section>
  );
}
