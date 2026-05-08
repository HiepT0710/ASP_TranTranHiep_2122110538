import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { registerSeller, registerUser } from "../services/apiService";
import { getApiErrorMessage } from "../utils/errorMessage";
import { isValidPhone, passwordStrength, validateRequired } from "../utils/formValidation";
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

    const current = mode === "User" ? userForm : sellerForm;
    const requiredFields = [
      { key: "username", value: current.username },
      { key: "password", value: current.password },
      { key: "fullName", value: current.fullName },
      { key: "email", value: current.email },
      { key: "phone", value: current.phone },
      { key: "address", value: current.address },
    ];
    if (mode === "Seller") requiredFields.push({ key: "restaurantName", value: current.restaurantName });

    const labels = {
      username: "Username",
      password: "Mật khẩu",
      fullName: "Họ tên",
      email: "Email",
      phone: "Số điện thoại",
      address: "Địa chỉ",
      restaurantName: "Tên quán",
    };

    const missing = validateRequired(requiredFields, labels);
    if (missing) {
      const message = `Vui lòng nhập ${missing}`;
      setMsg(message);
      setMsgType("error");
      pushToast(message, "error");
      return;
    }

    if (current.username.trim().length < 3 || current.username.trim().length > 30) {
      const message = "Username phải từ 3 đến 30 ký tự";
      setMsg(message);
      setMsgType("error");
      pushToast(message, "error");
      return;
    }

    const strength = passwordStrength(current.password);
    if (!strength.ok) {
      const message = "Mật khẩu phải có ít nhất 8 ký tự, gồm cả chữ và số";
      setMsg(message);
      setMsgType("error");
      pushToast(message, "error");
      return;
    }

    if (current.fullName.trim().length < 2) {
      const message = "Họ tên phải có ít nhất 2 ký tự";
      setMsg(message);
      setMsgType("error");
      pushToast(message, "error");
      return;
    }

    if (!isValidPhone(current.phone)) {
      const message = "Số điện thoại phải là số và có 9 đến 11 chữ số";
      setMsg(message);
      setMsgType("error");
      pushToast(message, "error");
      return;
    }

    if (mode === "User") {
      try {
        const res = await registerUser(userForm);
        const message = res.message || "Đăng ký user thành công";
        setMsg(message);
        setMsgType("ok");
        pushToast(message, "success");
      } catch (error) {
        const message = getApiErrorMessage(error, "Đăng ký thất bại");
        setMsg(message);
        setMsgType("error");
        pushToast(message, "error");
      }
    } else {
      if (sellerForm.restaurantName.trim().length < 3) {
        const message = "Tên quán phải có ít nhất 3 ký tự";
        setMsg(message);
        setMsgType("error");
        pushToast(message, "error");
        return;
      }
      try {
        const res = await registerSeller(sellerForm);
        const message = res.message || "Đăng ký seller thành công";
        setMsg(message);
        setMsgType("ok");
        pushToast(message, "success");
      } catch (error) {
        const message = getApiErrorMessage(error, "Đăng ký thất bại");
        setMsg(message);
        setMsgType("error");
        pushToast(message, "error");
      }
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
        <input placeholder="Username" minLength={3} maxLength={30} value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} />
        <input type="password" placeholder="Password" minLength={8} value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
        <div className="split">
          <input placeholder="Họ tên" minLength={2} value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} />
          <input placeholder="Email" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
        </div>
        <div className="split">
          <input placeholder="SĐT" inputMode="numeric" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value.replace(/\D/g, "") })} />
          <input placeholder="Địa chỉ" minLength={5} value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
        </div>
        {mode === "Seller" && <input placeholder="Tên quán" minLength={3} value={sellerForm.restaurantName} onChange={(e) => setSellerForm({ ...sellerForm, restaurantName: e.target.value })} />}
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
