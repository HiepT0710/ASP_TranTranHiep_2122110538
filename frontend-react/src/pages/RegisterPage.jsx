import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { registerSeller, registerUser } from "../services/apiService";
import { getApiErrorMessage } from "../utils/errorMessage";
import { useToast } from "../context/ToastContext";

const defaultUser = { username: "", password: "", fullName: "", email: "", phone: "", address: "" };
const defaultSeller = { ...defaultUser, restaurantName: "" };

export default function RegisterPage() {
  const { pushToast } = useToast();
  const [mode, setMode] = useState("User");
  const [msg, setMsg] = useState("");
  const [msgType, setMsgType] = useState("");
  const [userForm, setUserForm] = useState(defaultUser);
  const [sellerForm, setSellerForm] = useState(defaultSeller);

  const submit = async (e) => {
    e.preventDefault();
    setMsg("");
    setMsgType("");
    try {
      if (mode === "User") {
        const res = await registerUser(userForm);
        const message = res.message || "Đăng ký user thành công";
        setMsg(message);
        setMsgType("ok");
        pushToast(message, "success");
      } else {
        const res = await registerSeller(sellerForm);
        const message = res.message || "Đăng ký seller thành công";
        setMsg(message);
        setMsgType("ok");
        pushToast(message, "success");
      }
    } catch (error) {
      const message = getApiErrorMessage(error, "Đăng ký thất bại");
      setMsg(message);
      setMsgType("error");
      pushToast(message, "error");
    }
  };

  const form = mode === "User" ? userForm : sellerForm;
  const setForm = mode === "User" ? setUserForm : setSellerForm;
  const cardTitle = useMemo(() => (mode === "User" ? "Tạo tài khoản người dùng" : "Tạo tài khoản seller"), [mode]);

  return (
    <section className="page center-page">
      <div className="hero-card center-card panel center-hero">
      <p className="eyebrow">Đăng ký</p>
      <h2>{cardTitle}</h2>
      <p className="muted">Chọn loại tài khoản phù hợp để bắt đầu trải nghiệm hệ thống đặt món.</p>
      <div className="row">
        <button type="button" onClick={() => setMode("User")}>User</button>
        <button type="button" onClick={() => setMode("Seller")}>Seller</button>
      </div>
      <form className="form" onSubmit={submit}>
        <input placeholder="Username" value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} />
        <input type="password" placeholder="Password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
        <div className="split">
          <input placeholder="Họ tên" value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} />
          <input placeholder="Email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
        </div>
        <div className="split">
          <input placeholder="SĐT" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
          <input placeholder="Địa chỉ" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
        </div>
        {mode === "Seller" && <input placeholder="Tên quán" value={sellerForm.restaurantName} onChange={(e) => setSellerForm({ ...sellerForm, restaurantName: e.target.value })} />}
        <button type="submit">Đăng ký {mode}</button>
      </form>
      <div className="row" style={{ justifyContent: "space-between" }}>
        <span className="muted">Đã có tài khoản?</span>
        <Link to="/login">Đăng nhập</Link>
      </div>
      {msg && <p className={msgType === "error" ? "error" : "ok"}>{msg}</p>}
      </div>
    </section>
  );
}
