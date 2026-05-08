import { Link, NavLink } from "react-router-dom";
import NotificationBell from "./NotificationBell";
import ChatOrderStack from "./ChatOrderStack";
import ChatSupportLauncher from "./ChatSupportLauncher";
import ChatMiniPanel from "./ChatMiniPanel";
import { useChat } from "../context/ChatContext";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";

const ICONS = {
  home: "⌂",
  restaurant: "🍽",
  food: "🥗",
  cart: "🛒",
  orders: "📦",
  account: "👤",
  login: "↪",
  register: "✚",
  logout: "⎋",
  seller: "🧑‍🍳",
  admin: "🛠",
};

function roleName(role) {
  if (!role) return "Guest";
  return role;
}

export default function Layout({ children }) {
  const { user, isAuthenticated, logout } = useAuth();
  const { pushToast } = useToast();
  const { supportOpen } = useChat();

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="brand-wrap">
          <Link to="/" className="brand">Xì mi food</Link>
          <span className="badge">{roleName(user?.role)}</span>
        </div>
        <nav className="menu">
          <NavLink to="/">{ICONS.home} Trang chủ</NavLink>
          <NavLink to="/restaurants">{ICONS.restaurant} Quán ăn</NavLink>
          <NavLink to="/foods">{ICONS.food} Món ăn</NavLink>
          <NavLink to="/cart">{ICONS.cart} Giỏ hàng</NavLink>
          {isAuthenticated && <NavLink to="/orders">{ICONS.orders} Đơn của tôi</NavLink>}
          {isAuthenticated && <NavLink to="/profile">{ICONS.account} Tài khoản</NavLink>}
          {user?.role === "Seller" && <NavLink to="/seller">{ICONS.seller} Seller</NavLink>}
          {user?.role === "Admin" && <NavLink to="/admin">{ICONS.admin} Admin</NavLink>}
        </nav>
        <div className="auth-area">
          {isAuthenticated && <NotificationBell />}
          {!isAuthenticated ? (
            <>
              <Link to="/login" className="secondary icon-btn button-link">{ICONS.login} Đăng nhập</Link>
              <Link to="/register" className="icon-btn button-link">{ICONS.register} Đăng ký</Link>
            </>
          ) : (
            <button
              type="button"
              className="link-btn icon-btn"
              onClick={async () => {
                await logout();
                pushToast("Đăng xuất thành công", "success");
              }}
            >
              {ICONS.logout} Đăng xuất
            </button>
          )}
        </div>
      </header>
      <main>{children}</main>
      {isAuthenticated && <ChatSupportLauncher />}
      {supportOpen && <ChatMiniPanel />}
      {isAuthenticated && <ChatOrderStack />}
      <footer className="footer">© {new Date().getFullYear()} FoodOrder Platform · Trải nghiệm đặt món hiện đại</footer>
    </div>
  );
}
