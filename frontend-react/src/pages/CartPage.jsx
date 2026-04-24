import { useEffect, useMemo, useState } from "react";
import { checkout, clearCart, getCart, removeFromCart, updateCart } from "../services/apiService";
import { useToast } from "../context/ToastContext";

export default function CartPage() {
  const { pushToast } = useToast();
  const [cart, setCart] = useState({ items: [] });
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState("");
  const [checkoutForm, setCheckoutForm] = useState({ paymentMethod: "COD", address: "", phone: "", voucherCode: "" });

  const loadData = async () => {
    setLoading(true);
    const data = await getCart();
    setCart(data);
    setLoading(false);
  };

  useEffect(() => {
    loadData();
  }, []);

  const updateQty = async (foodId, quantity) => {
    await updateCart({ foodId, quantity });
    pushToast("Đã cập nhật số lượng", "info");
    loadData();
  };

  const subtotal = useMemo(() => cart.subtotal || 0, [cart.subtotal]);
  const voucherText = checkoutForm.voucherCode.trim();

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

  const doCheckout = async () => {
    try {
      const res = await checkout(checkoutForm);
      const message = `Đặt hàng thành công. Mã đơn: ${res.orderId}`;
      setMsg(message);
      pushToast(message, "success");
      await loadData();
    } catch (error) {
      const message = error?.response?.data?.message || "Checkout thất bại";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  return (
    <section className="page">
      <div className="split">
        <div>
          <p className="eyebrow">Giỏ hàng</p>
          <h2>Kiểm tra món và đặt hàng</h2>
          {msg && <p className="ok">{msg}</p>}
          {loading ? (
            <div className="panel">Đang tải giỏ hàng...</div>
          ) : cart.items?.length ? (
            <table className="table">
              <thead>
                <tr>
                  <th>Món</th>
                  <th>Giá</th>
                  <th>SL</th>
                  <th>Tổng</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {cart.items?.map((x) => (
                  <tr key={x.id}>
                    <td>{x.name}</td>
                    <td>{x.price}</td>
                    <td>
                      <input type="number" min="1" defaultValue={x.quantity} onBlur={(e) => updateQty(x.id, Number(e.target.value))} />
                    </td>
                    <td>{x.lineTotal}</td>
                    <td><button onClick={() => removeItem(x.id)}>Xóa</button></td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <div className="panel soft-panel">Giỏ hàng đang trống. Hãy thêm món từ trang món ăn.</div>
          )}
          <div className="row">
            <button onClick={clearAll}>Xóa toàn bộ giỏ</button>
          </div>
        </div>
        <aside className="panel" style={{ alignSelf: "start" }}>
          <h3>Checkout</h3>
          <p className="muted">Tạm tính: <b>{subtotal}</b></p>
          <p className="muted">Voucher: <b>{voucherText || "Chưa nhập"}</b></p>
          <div className="panel soft-panel" style={{ marginBottom: 12 }}>
            <p className="muted" style={{ marginBottom: 6 }}>Ước tính sau voucher</p>
            <b>{subtotal}</b>
            <p className="muted" style={{ marginBottom: 0, marginTop: 8 }}>
              Khi đặt hàng, hệ thống sẽ tự tính mức giảm hợp lệ theo promotion gắn với voucher.
            </p>
          </div>
          <div className="form">
            <select value={checkoutForm.paymentMethod} onChange={(e) => setCheckoutForm({ ...checkoutForm, paymentMethod: e.target.value })}>
              <option value="COD">COD</option>
              <option value="VNPay">VNPay</option>
              <option value="MoMo">MoMo</option>
            </select>
            <input placeholder="Địa chỉ nhận hàng" value={checkoutForm.address} onChange={(e) => setCheckoutForm({ ...checkoutForm, address: e.target.value })} />
            <input placeholder="Số điện thoại" value={checkoutForm.phone} onChange={(e) => setCheckoutForm({ ...checkoutForm, phone: e.target.value })} />
            <input placeholder="Nhập voucher nếu có" value={checkoutForm.voucherCode} onChange={(e) => setCheckoutForm({ ...checkoutForm, voucherCode: e.target.value })} />
            <button onClick={doCheckout}>Đặt hàng</button>
          </div>
        </aside>
      </div>
    </section>
  );
}
