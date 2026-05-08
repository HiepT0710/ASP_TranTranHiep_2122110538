import { useEffect, useMemo, useState } from "react";
import { checkout, clearCart, getCart, getProfile, getSystemSettings, getVoucherSuggestions, removeFromCart, simulateOnlinePayment, updateCart } from "../services/apiService";
import { useAuth } from "../context/AuthContext";
import { isValidPhone, validateRequired } from "../utils/formValidation";
import { useToast } from "../context/ToastContext";

const buildMomoTestQrUrl = (orderId, amount) => {
  const payload = [
    "MOMO_TEST",
    `order=${orderId}`,
    `amount=${Number(amount || 0)}`,
    `note=Thanh toan test khong mat tien`,
  ].join(" |");
  return `https://api.qrserver.com/v1/create-qr-code/?size=260x260&data=${encodeURIComponent(payload)}`;
};

export default function CartPage() {
  const { user } = useAuth();
  const { pushToast } = useToast();
  const [cart, setCart] = useState({ items: [] });
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState("");
  const [checkoutForm, setCheckoutForm] = useState({ paymentMethod: "COD", address: "", phone: "", voucherCode: "" });
  const [voucherData, setVoucherData] = useState({ items: [], total: 0 });
  const [voucherLoading, setVoucherLoading] = useState(false);
  const [autoVoucher, setAutoVoucher] = useState(false);
  const [momoQr, setMomoQr] = useState(null);
  const [momoOrder, setMomoOrder] = useState(null);
  const [momoPending, setMomoPending] = useState(false);
  const [momoPaid, setMomoPaid] = useState(false);
  const [systemSettings, setSystemSettings] = useState({ defaultFee: 20000, freeShipThreshold: 100000 });

  const loadData = async () => {
    setLoading(true);
    try {
      const data = await getCart();
      setCart(data);
    } finally {
      setLoading(false);
    }
  };

  const loadVoucherSuggestions = async () => {
    setVoucherLoading(true);
    try {
      const data = await getVoucherSuggestions();
      const items = (data.items || []).map((item) => ({
        ...item,
        statusColor: item.canUse ? "green" : item.reasons?.length ? "amber" : "gray",
      }));
      setVoucherData({ items, total: data.total || 0 });
      if (autoVoucher && items.length) {
        const best = [...items].sort((a, b) => {
          if (a.canUse !== b.canUse) return a.canUse ? -1 : 1;
          if ((b.estimatedDiscount || 0) !== (a.estimatedDiscount || 0)) return (b.estimatedDiscount || 0) - (a.estimatedDiscount || 0);
          return (a.remainingAmount || 0) - (b.remainingAmount || 0);
        })[0];
        if (best?.code) {
          setCheckoutForm((current) => ({ ...current, voucherCode: best.code }));
        }
      }
    } catch {
      setVoucherData({ items: [], total: 0 });
    } finally {
      setVoucherLoading(false);
    }
  };

  useEffect(() => {
    loadData();
    loadVoucherSuggestions();
    getSystemSettings().then((data) => {
      const map = Object.fromEntries((data.items || []).map((item) => [item.key, item.value]));
      setSystemSettings({
        defaultFee: Number(map["Shipping:DefaultFee"] || 20000),
        freeShipThreshold: Number(map["Shipping:FreeShipThreshold"] || 100000),
      });
    }).catch(() => setSystemSettings({ defaultFee: 20000, freeShipThreshold: 100000 }));
  }, []);

  useEffect(() => {
    const syncProfileContact = async () => {
      try {
        const profile = await getProfile();
        setCheckoutForm((current) => ({
          ...current,
          address: current.address || profile?.address || user?.address || "",
          phone: current.phone || profile?.phone || user?.phone || "",
        }));
      } catch {
        setCheckoutForm((current) => ({
          ...current,
          address: current.address || user?.address || "",
          phone: current.phone || user?.phone || "",
        }));
      }
    };
    syncProfileContact();
  }, [user]);

  const resetContactInfo = async () => {
    try {
      const profile = await getProfile();
      setCheckoutForm((current) => ({
        ...current,
        address: profile?.address || user?.address || "",
        phone: profile?.phone || user?.phone || "",
      }));
    } catch {
      setCheckoutForm((current) => ({
        ...current,
        address: user?.address || "",
        phone: user?.phone || "",
      }));
    }
    pushToast("Đã lấy lại địa chỉ và số điện thoại từ hồ sơ", "info");
  };

  const updateQty = async (foodId, quantity) => {
    await updateCart({ foodId, quantity });
    pushToast("Đã cập nhật số lượng", "info");
    loadData();
  };

  const subtotal = useMemo(() => cart.subtotal || 0, [cart.subtotal]);
  const voucherText = checkoutForm.voucherCode.trim();
  const selectedVoucher = useMemo(
    () => voucherData.items.find((item) => item.code?.toUpperCase() === voucherText.toUpperCase()),
    [voucherData.items, voucherText]
  );
  const bestVoucher = useMemo(() => {
    if (!voucherData.items.length) return null;
    return [...voucherData.items].sort((a, b) => {
      if (a.canUse !== b.canUse) return a.canUse ? -1 : 1;
      if ((b.estimatedDiscount || 0) !== (a.estimatedDiscount || 0)) return (b.estimatedDiscount || 0) - (a.estimatedDiscount || 0);
      if ((a.remainingAmount || 0) !== (b.remainingAmount || 0)) return (a.remainingAmount || 0) - (b.remainingAmount || 0);
      return String(a.code || "").localeCompare(String(b.code || ""));
    })[0];
  }, [voucherData.items]);
  const previewSubtotal = selectedVoucher?.canUse ? Math.max(0, subtotal - (selectedVoucher.estimatedDiscount || 0)) : subtotal;
  const defaultShippingFee = Number(systemSettings.defaultFee || 20000);
  const freeShipThreshold = Number(systemSettings.freeShipThreshold || 100000);
  const estimatedShippingFee = previewSubtotal >= freeShipThreshold ? 0 : defaultShippingFee;
  const amountToFreeShip = Math.max(0, freeShipThreshold - previewSubtotal);
  const estimatedGrandTotal = previewSubtotal + estimatedShippingFee;

  const removeItem = async (foodId) => {
    await removeFromCart(foodId);
    pushToast("Đã xóa món khỏi giỏ", "info");
    loadData();
  };

  const clearAll = async () => {
    await clearCart();
    pushToast("Đã xóa toàn bộ giỏ hàng", "info");
    loadData();
  };

  const confirmMomoPaid = async () => {
    if (!momoOrder?.orderId) return;
    try {
      await simulateOnlinePayment(momoOrder.orderId);
      setMomoPaid(true);
      setMomoPending(false);
      setMsg(`Đơn #${momoOrder.orderId} đã được xác nhận thanh toán MoMo test.`);
      pushToast(`Đơn #${momoOrder.orderId} đã được xác nhận thanh toán MoMo test.`, "success");
      await loadData();
    } catch (error) {
      const message = error?.response?.data?.message || "Xác nhận thanh toán thất bại";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  const doCheckout = async () => {
    const missing = validateRequired([
      { key: "paymentMethod", value: checkoutForm.paymentMethod },
      { key: "address", value: checkoutForm.address },
      { key: "phone", value: checkoutForm.phone },
    ], { paymentMethod: "Phương thức thanh toán", address: "Địa chỉ nhận hàng", phone: "Số điện thoại" });
    if (missing) {
      const message = `Vui lòng nhập ${missing}`;
      setMsg(message);
      pushToast(message, "error");
      return;
    }
    if (!isValidPhone(checkoutForm.phone)) {
      const message = "Số điện thoại phải là số và có 9 đến 11 chữ số";
      setMsg(message);
      pushToast(message, "error");
      return;
    }
    try {
      const res = await checkout(checkoutForm);
      const isVnpay = checkoutForm.paymentMethod === "VNPay";
      const isMomo = checkoutForm.paymentMethod === "MoMo";
      if (isVnpay && res.paymentUrl) {
        setMsg(`Đơn đã tạo. VNPay sandbox demo đã sẵn sàng. Mã đơn: ${res.orderId}`);
        pushToast(`Đơn đã tạo. VNPay sandbox demo đã sẵn sàng. Mã đơn: ${res.orderId}`, "success");
        window.open(res.paymentUrl, "_blank", "noopener,noreferrer");
      } else {
        const message = isMomo
          ? `Đơn đã tạo. Hãy quét QR MoMo test rồi bấm xác nhận mô phỏng. Mã đơn: ${res.orderId}`
          : `Đặt hàng thành công. Mã đơn: ${res.orderId}`;
        setMsg(message);
        pushToast(message, "success");
      }
      setMomoOrder(res);
      setMomoPaid(false);
      if (isMomo) {
        setMomoPending(true);
        setMomoQr(buildMomoTestQrUrl(res.orderId, res.total));
      } else {
        setMomoPending(false);
        setMomoQr(null);
      }
      await loadData();
    } catch (error) {
      const message = error?.response?.data?.message || "Checkout thất bại";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  return (
    <section className="page cart-page">
      <div className="cart-layout">
        <div className="cart-main">
          <div className="cart-header">
            <div>
              <p className="eyebrow">Giỏ hàng</p>
              <h2>Kiểm tra món và đặt hàng</h2>
              <p className="muted">Tối ưu lại để xem nhanh đơn hàng, phí ship và thanh toán mà không bị kéo quá dài.</p>
            </div>
            <div className="cart-actions-top">
              <button type="button" className="secondary" onClick={clearAll}>Xóa toàn bộ giỏ</button>
            </div>
          </div>

          {msg && <p className="ok cart-message">{msg}</p>}

          {loading ? (
            <div className="panel">Đang tải giỏ hàng...</div>
          ) : cart.items?.length ? (
            <div className="cart-items panel">
              <div className="cart-items-header">
                <span>Món</span>
                <span>Đơn giá</span>
                <span>Số lượng</span>
                <span>Tổng</span>
                <span />
              </div>
              {cart.items?.map((x) => (
                <div key={x.id} className="cart-item-row">
                  <div className="cart-item-name">{x.name}</div>
                  <div className="cart-item-price">{Number(x.price || 0).toLocaleString()} đ</div>
                  <div className="cart-item-qty">
                    <input type="number" min="1" defaultValue={x.quantity} onBlur={(e) => updateQty(x.id, Number(e.target.value))} />
                  </div>
                  <div className="cart-item-total">{Number(x.lineTotal || 0).toLocaleString()} đ</div>
                  <div className="cart-item-remove"><button type="button" className="secondary" onClick={() => removeItem(x.id)}>Xóa</button></div>
                </div>
              ))}
            </div>
          ) : (
            <div className="panel soft-panel">Giỏ hàng đang trống. Hãy thêm món từ trang món ăn.</div>
          )}
        </div>

        <aside className="cart-sidebar panel">
          <div className="cart-summary-card">
            <div className="cart-summary-head">
              <h3>Thanh toán</h3>
              <span className="badge">Ước tính</span>
            </div>

            <div className="cart-summary-grid">
              <div><span>Tạm tính</span><strong>{subtotal.toLocaleString()} đ</strong></div>
              <div><span>Sau voucher</span><strong>{previewSubtotal.toLocaleString()} đ</strong></div>
              <div><span>Phí ship</span><strong>{estimatedShippingFee.toLocaleString()} đ</strong></div>
              <div><span>Tổng cuối</span><strong>{estimatedGrandTotal.toLocaleString()} đ</strong></div>
            </div>

            <div className="cart-summary-note">
              {selectedVoucher?.canUse
                ? `Voucher ${selectedVoucher.code} giảm khoảng ${Number(selectedVoucher.estimatedDiscount || 0).toLocaleString()} đ.`
                : bestVoucher?.canUse
                  ? `Voucher tốt nhất hiện tại: ${bestVoucher.code}`
                  : `Còn thiếu ${amountToFreeShip.toLocaleString()} đ để được miễn phí ship.`}
            </div>

            <button type="button" className="secondary cart-full-btn" onClick={resetContactInfo}>Lấy lại địa chỉ / SĐT từ hồ sơ</button>
          </div>

          <div className="cart-form panel soft-panel">
            <div className="form-field">
              <span>Phương thức thanh toán</span>
              <select value={checkoutForm.paymentMethod} onChange={(e) => setCheckoutForm({ ...checkoutForm, paymentMethod: e.target.value })}>
                <option value="COD">COD</option>
                <option value="VNPay">VNPay (sandbox)</option>
                <option value="MoMo">MoMo (test QR)</option>
              </select>
            </div>
            <div className="form-field">
              <span>Địa chỉ nhận hàng</span>
              <div className="cart-inline-input">
                <input placeholder="Địa chỉ nhận hàng" value={checkoutForm.address} onChange={(e) => setCheckoutForm({ ...checkoutForm, address: e.target.value })} />
                <button type="button" className="secondary" onClick={() => setCheckoutForm((current) => ({ ...current, address: "" }))}>Xóa</button>
              </div>
            </div>
            <div className="form-field">
              <span>Số điện thoại</span>
              <div className="cart-inline-input">
                <input inputMode="numeric" placeholder="Số điện thoại" value={checkoutForm.phone} onChange={(e) => setCheckoutForm({ ...checkoutForm, phone: e.target.value.replace(/\D/g, "") })} />
                <button type="button" className="secondary" onClick={() => setCheckoutForm((current) => ({ ...current, phone: "" }))}>Xóa</button>
              </div>
            </div>
            <div className="form-field">
              <span>Voucher</span>
              <div className="cart-inline-input">
                <input placeholder="Nhập voucher nếu có" value={checkoutForm.voucherCode} onChange={(e) => setCheckoutForm({ ...checkoutForm, voucherCode: e.target.value.toUpperCase() })} />
                <button type="button" className="secondary" onClick={() => selectedVoucher?.code && setCheckoutForm((current) => ({ ...current, voucherCode: selectedVoucher.code }))} disabled={!selectedVoucher?.canUse}>Dùng</button>
              </div>
            </div>
            <div className="cart-inline-actions">
              <button type="button" className="secondary" onClick={() => setAutoVoucher((current) => !current)}>{autoVoucher ? "Tắt chọn tốt nhất" : "Chọn voucher tốt nhất"}</button>
              <button type="button" className="secondary" onClick={clearAll}>Xóa giỏ</button>
              <button onClick={doCheckout}>Đặt hàng</button>
            </div>
          </div>

          <div className="cart-info panel">
            <div className="cart-info-head">
              <h4>Voucher khả dụng</h4>
              {bestVoucher?.canUse && <span className="badge">Tốt nhất: {bestVoucher.code}</span>}
            </div>
            {voucherLoading ? (
              <p className="muted">Đang tải voucher...</p>
            ) : voucherData.items.length === 0 ? (
              <p className="muted">Không có voucher phù hợp cho đơn hàng hiện tại.</p>
            ) : (
              <div className="cart-voucher-list">
                {voucherData.items.slice(0, 3).map((v) => (
                  <button
                    key={v.Id || v.id || v.code}
                    type="button"
                    className={`cart-voucher-chip ${v.canUse ? "is-usable" : ""}`}
                    onClick={() => v.canUse && setCheckoutForm((current) => ({ ...current, voucherCode: v.code }))}
                  >
                    <strong>{v.code}</strong>
                    <span>{Number(v.estimatedDiscount || 0).toLocaleString()} đ</span>
                  </button>
                ))}
              </div>
            )}
          </div>

          {momoQr && momoOrder && (
            <div className="panel soft-panel cart-momo-box">
              <h4>Thanh toán test</h4>
              <p className="muted">Đơn đã được tạo, chưa thanh toán.</p>
              <img src={momoQr} alt="MoMo test QR" />
              <p className="muted">Đơn #{momoOrder.orderId} - Số tiền: <b>{Number(momoOrder.total || 0).toLocaleString()} đ</b></p>
              <button type="button" className="secondary" onClick={confirmMomoPaid} disabled={!momoPending || momoPaid}>
                {momoPaid ? "Đã xác nhận thanh toán" : "Xác nhận đã quét xong"}
              </button>
            </div>
          )}
        </aside>
      </div>
    </section>
  );
}
