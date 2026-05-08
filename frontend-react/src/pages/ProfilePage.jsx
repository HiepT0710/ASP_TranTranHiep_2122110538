import { useEffect, useMemo, useState } from "react";
import {
  changePassword,
  forgotPassword,
  getProfile,
  resolveImageUrl,
  updateAvatar,
  updateProfile,
} from "../services/apiService";
import { isValidPhone, validateRequired } from "../utils/formValidation";
import { useToast } from "../context/ToastContext";

export default function ProfilePage() {
  const { pushToast } = useToast();
  const [profile, setProfile] = useState(null);
  const [profileForm, setProfileForm] = useState({ fullName: "", email: "", phone: "", address: "" });
  const [passwordForm, setPasswordForm] = useState({ currentPassword: "", newPassword: "", confirmPassword: "" });
  const [avatarFile, setAvatarFile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [avatarBusy, setAvatarBusy] = useState(false);
  const [profileBusy, setProfileBusy] = useState(false);
  const [passwordBusy, setPasswordBusy] = useState(false);
  const [resetBusy, setResetBusy] = useState(false);
  const [previewUrl, setPreviewUrl] = useState("");

  const initials = useMemo(() => (profile?.fullName || profile?.username || "U").slice(0, 1).toUpperCase(), [profile]);

  const loadData = async () => {
    setLoading(true);
    const data = await getProfile();
    setProfile(data);
    setProfileForm({
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

  useEffect(() => {
    if (!avatarFile) return setPreviewUrl("");
    const url = URL.createObjectURL(avatarFile);
    setPreviewUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [avatarFile]);

  const submitProfile = async (e) => {
    e.preventDefault();
    const missing = validateRequired(
      [
        { key: "fullName", value: profileForm.fullName },
        { key: "email", value: profileForm.email },
      ],
      { fullName: "Họ tên", email: "Email" }
    );
    if (missing) return pushToast(`Vui lòng nhập ${missing}`, "error");
    if (profileForm.phone && !isValidPhone(profileForm.phone)) return pushToast("Số điện thoại phải là số và có 9 đến 11 chữ số", "error");

    setProfileBusy(true);
    try {
      const res = await updateProfile(profileForm);
      pushToast(res.message || "Đã cập nhật thông tin", "success");
      await loadData();
    } finally {
      setProfileBusy(false);
    }
  };

  const submitAvatar = async (e) => {
    e.preventDefault();
    if (!avatarFile) return pushToast("Vui lòng chọn ảnh avatar", "error");

    setAvatarBusy(true);
    try {
      const res = await updateAvatar(avatarFile);
      pushToast(res.message || "Đã cập nhật avatar", "success");
      setAvatarFile(null);
      await loadData();
    } finally {
      setAvatarBusy(false);
    }
  };

  const submitPassword = async (e) => {
    e.preventDefault();
    if (!passwordForm.currentPassword || !passwordForm.newPassword || !passwordForm.confirmPassword) {
      return pushToast("Vui lòng nhập đầy đủ thông tin đổi mật khẩu", "error");
    }
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      return pushToast("Mật khẩu mới và xác nhận mật khẩu không khớp", "error");
    }

    setPasswordBusy(true);
    try {
      const res = await changePassword(passwordForm);
      pushToast(res.message || "Đã đổi mật khẩu", "success");
      setPasswordForm({ currentPassword: "", newPassword: "", confirmPassword: "" });
    } finally {
      setPasswordBusy(false);
    }
  };

  const resendResetEmail = async () => {
    if (!profile?.email) return pushToast("Không có email để gửi lại", "error");
    setResetBusy(true);
    try {
      const res = await forgotPassword({ email: profile.email });
      pushToast(res.message || "Đã gửi lại email reset", "success");
    } finally {
      setResetBusy(false);
    }
  };

  if (loading) return <section className="page">Đang tải hồ sơ cá nhân...</section>;

  const avatarSrc = previewUrl || (profile?.avatarUrl ? resolveImageUrl(profile.avatarUrl) : "");

  return (
    <section className="page profile-page">
      <div className="profile-hero panel">
        <div className="profile-avatar-wrap">
          {avatarSrc ? (
            <img src={avatarSrc} alt={profile?.fullName || profile?.username || "avatar"} className="profile-avatar" />
          ) : (
            <div className="profile-avatar placeholder">{initials}</div>
          )}
        </div>
        <div className="profile-hero-copy">
          <p className="eyebrow">Tài khoản</p>
          <h2>Hồ sơ cá nhân</h2>
          <p className="muted">Quản lý thông tin liên hệ, ảnh đại diện và bảo mật tài khoản trong một nơi.</p>
          <div className="profile-meta">
            <span className="badge">{profile?.role}</span>
            <span className="muted">Tạo lúc: {profile?.createdAt ? new Date(profile.createdAt).toLocaleDateString("vi-VN") : "-"}</span>
          </div>
          <button className="secondary-btn" type="button" onClick={resendResetEmail} disabled={resetBusy}>
            {resetBusy ? "Đang gửi..." : "Gửi lại email reset"}
          </button>
        </div>
      </div>

      <div className="profile-grid">
        <form className="panel form" onSubmit={submitProfile}>
          <h3>Thông tin cơ bản</h3>
          <input value={profileForm.fullName} onChange={(e) => setProfileForm({ ...profileForm, fullName: e.target.value })} placeholder="Họ tên" />
          <input value={profileForm.email} type="email" onChange={(e) => setProfileForm({ ...profileForm, email: e.target.value })} placeholder="Email" />
          <div className="split">
            <input value={profileForm.phone} inputMode="numeric" onChange={(e) => setProfileForm({ ...profileForm, phone: e.target.value.replace(/\D/g, "") })} placeholder="SĐT" />
            <input value={profileForm.address} onChange={(e) => setProfileForm({ ...profileForm, address: e.target.value })} placeholder="Địa chỉ" />
          </div>
          <button type="submit" disabled={profileBusy}>{profileBusy ? "Đang lưu..." : "Lưu thay đổi"}</button>
        </form>

        <form className="panel form" onSubmit={submitAvatar}>
          <h3>Ảnh đại diện</h3>
          <input type="file" accept="image/*" onChange={(e) => setAvatarFile(e.target.files?.[0] || null)} />
          {avatarFile && <p className="muted">Đã chọn: {avatarFile.name}</p>}
          <button type="submit" disabled={avatarBusy}>{avatarBusy ? "Đang tải..." : "Cập nhật avatar"}</button>
        </form>

        <form className="panel form" onSubmit={submitPassword}>
          <h3>Đổi mật khẩu</h3>
          <input type="password" placeholder="Mật khẩu hiện tại" value={passwordForm.currentPassword} onChange={(e) => setPasswordForm({ ...passwordForm, currentPassword: e.target.value })} />
          <input type="password" placeholder="Mật khẩu mới" value={passwordForm.newPassword} onChange={(e) => setPasswordForm({ ...passwordForm, newPassword: e.target.value })} />
          <input type="password" placeholder="Nhập lại mật khẩu mới" value={passwordForm.confirmPassword} onChange={(e) => setPasswordForm({ ...passwordForm, confirmPassword: e.target.value })} />
          <button type="submit" disabled={passwordBusy}>{passwordBusy ? "Đang đổi..." : "Đổi mật khẩu"}</button>
        </form>
      </div>
    </section>
  );
}
