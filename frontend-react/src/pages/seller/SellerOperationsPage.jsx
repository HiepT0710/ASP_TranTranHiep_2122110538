import { useEffect, useMemo, useState } from "react";
import { useToast } from "../../context/ToastContext";
import {
  getSellerRestaurantOperations,
  updateSellerRestaurantState,
  upsertSellerOperatingHour,
} from "../../services/apiService";

const WEEKDAYS = [
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
  "Sunday",
];

const weekdayLabels = {
  Monday: "Thứ 2",
  Tuesday: "Thứ 3",
  Wednesday: "Thứ 4",
  Thursday: "Thứ 5",
  Friday: "Thứ 6",
  Saturday: "Thứ 7",
  Sunday: "Chủ nhật",
};

export default function SellerOperationsPage() {
  const { pushToast } = useToast();
  const [data, setData] = useState(null);
  const [form, setForm] = useState({ isOpen: true, isAcceptingOrders: true, openingHours: "", statusNote: "" });
  const [hours, setHours] = useState({});

  const load = async () => {
    try {
      const res = await getSellerRestaurantOperations();
      setData(res);
      setForm({
        isOpen: !!res.isOpen,
        isAcceptingOrders: !!res.isAcceptingOrders,
        openingHours: res.openingHours || "",
        statusNote: res.statusNote || "",
      });
      const mapped = {};
      (res.hours || []).forEach((x) => { mapped[x.dayOfWeek] = x; });
      setHours(mapped);
    } catch (error) {
      pushToast(error?.response?.data?.message || "Không tải được trạng thái quán", "error");
    }
  };

  useEffect(() => { load(); }, []);

  const saveState = async () => {
    try {
      await updateSellerRestaurantState(form);
      pushToast("Đã cập nhật trạng thái quán", "success");
      load();
    } catch (error) {
      pushToast(error?.response?.data?.message || "Không lưu được trạng thái", "error");
    }
  };

  const saveHour = async (dayOfWeek) => {
    const item = hours[dayOfWeek] || {};
    try {
      await upsertSellerOperatingHour({
        dayOfWeek,
        openTime: item.openTime || "",
        closeTime: item.closeTime || "",
        isClosed: !!item.isClosed,
        note: item.note || "",
      });
      pushToast(`Đã lưu giờ ${weekdayLabels[dayOfWeek] || dayOfWeek}`, "success");
      load();
    } catch (error) {
      pushToast(error?.response?.data?.message || "Không lưu được giờ hoạt động", "error");
    }
  };

  const statusText = useMemo(() => {
    if (!form.isOpen) return "Quán đang đóng";
    if (!form.isAcceptingOrders) return "Tạm ngưng nhận đơn";
    return "Đang mở và nhận đơn";
  }, [form]);

  return (
    <section className="page">
      <p className="eyebrow">Seller vận hành</p>
      <h2>Quản lý trạng thái quán</h2>
      <div className="panel soft-panel" style={{ marginBottom: 16 }}>
        <h3>{data?.name}</h3>
        <p className="muted">{statusText}</p>
        <div className="row" style={{ flexWrap: "wrap" }}>
          <label><input type="checkbox" checked={form.isOpen} onChange={(e) => setForm({ ...form, isOpen: e.target.checked })} /> Mở quán</label>
          <label><input type="checkbox" checked={form.isAcceptingOrders} onChange={(e) => setForm({ ...form, isAcceptingOrders: e.target.checked })} /> Nhận đơn</label>
        </div>
        <textarea rows={3} placeholder="Ghi chú trạng thái / lý do tạm ngưng" value={form.statusNote} onChange={(e) => setForm({ ...form, statusNote: e.target.value })} />
        <textarea rows={3} placeholder="Ví dụ: T2-T6 08:00-22:00; T7-CN 09:00-23:00" value={form.openingHours} onChange={(e) => setForm({ ...form, openingHours: e.target.value })} />
        <button onClick={saveState}>Lưu trạng thái quán</button>
      </div>

      <div className="panel">
        <h3>Giờ hoạt động theo ngày</h3>
        <div className="dashboard-summary-grid">
          {WEEKDAYS.map((day) => {
            const item = hours[day] || {};
            return (
              <div key={day} className="panel soft-panel">
                <strong>{weekdayLabels[day]}</strong>
                <label className="row"><input type="checkbox" checked={!!item.isClosed} onChange={(e) => setHours({ ...hours, [day]: { ...item, isClosed: e.target.checked } })} /> Đóng ngày này</label>
                <input type="time" value={item.openTime || ""} onChange={(e) => setHours({ ...hours, [day]: { ...item, openTime: e.target.value } })} />
                <input type="time" value={item.closeTime || ""} onChange={(e) => setHours({ ...hours, [day]: { ...item, closeTime: e.target.value } })} />
                <input placeholder="Ghi chú" value={item.note || ""} onChange={(e) => setHours({ ...hours, [day]: { ...item, note: e.target.value } })} />
                <button className="secondary" onClick={() => saveHour(day)}>Lưu</button>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
