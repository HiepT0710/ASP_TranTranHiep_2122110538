import { useEffect, useState } from "react";
import { getProfile, updateProfile } from "../services/apiService";
import { useToast } from "../context/ToastContext";

export default function ProfilePage() {
  const { pushToast } = useToast();
  const [form, setForm] = useState({ fullName: "", email: "", phone: "", address: "" });
  const [msg, setMsg] = useState("");
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    setLoading(true);
    const data = await getProfile();
    setForm({
      fullName: data.fullName || "",
      email: data.email || "",
      phone: data.phone || "",
      address: data.address || "",
    });
    setLoading(false);
  };

  useEffect(() => {
    loadData();
  }, []);

  const submit = async (e) => {
    e.preventDefault();
    const res = await updateProfile(form);
    const message = res.message || "Đã cập nhật";
    setMsg(message);
    pushToast(message, "success");
  };

  if (loading) return <section className="page">Đang tải hồ sơ cá nhân...</section>;

  return (
    <section className="page center-page">
      <div className="hero-card center-card panel center-hero">
      <p className="eyebrow">Tài khoản</p>
      <h2>Hồ sơ cá nhân</h2>
      <p className="muted">Cập nhật thông tin liên hệ để hỗ trợ giao hàng và xử lý đơn nhanh hơn.</p>
      <form className="form" onSubmit={submit}>
        <input value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} placeholder="Họ tên" />
        <input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} placeholder="Email" />
        <div className="split">
          <input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} placeholder="SĐT" />
          <input value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} placeholder="Địa chỉ" />
        </div>
        <button type="submit">Lưu thay đổi</button>
      </form>
      {msg && <p className="ok">{msg}</p>}
      </div>
    </section>
  );
}
