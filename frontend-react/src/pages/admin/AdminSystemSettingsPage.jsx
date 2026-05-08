import { useEffect, useMemo, useState } from "react";
import { api } from "../../api";

const DEFAULT_KEYS = [
  { key: "Shipping:DefaultFee", label: "Giá ship mặc định", description: "Phí ship mặc định cho mỗi đơn hàng" },
  { key: "Shipping:FreeShipThreshold", label: "Mốc miễn ship", description: "Từ giá trị đơn hàng này trở lên thì miễn phí ship" },
  { key: "Order:CancelWindowMinutes", label: "Thời gian hủy đơn", description: "Số phút cho phép khách hủy đơn sau khi đặt" },
];

export default function AdminSystemSettingsPage() {
  const [items, setItems] = useState([]);
  const [form, setForm] = useState({ key: DEFAULT_KEYS[0].key, value: "", description: DEFAULT_KEYS[0].description });

  const selectedDefault = useMemo(
    () => DEFAULT_KEYS.find((item) => item.key === form.key),
    [form.key]
  );

  const load = async () => {
    const res = await api.get("/Admin/Settings/Index");
    setItems(res.data.items || []);
  };

  useEffect(() => { load(); }, []);

  const applyDefaultKey = (key) => {
    const preset = DEFAULT_KEYS.find((item) => item.key === key);
    setForm((cur) => ({
      ...cur,
      key,
      description: preset?.description ?? cur.description,
    }));
  };

  const submit = async (e) => {
    e.preventDefault();
    if (!form.key.trim()) return;
    await api.put("/Admin/Settings/Upsert", form);
    setForm({ key: DEFAULT_KEYS[0].key, value: "", description: DEFAULT_KEYS[0].description });
    await load();
  };

  return (
    <section className="page panel">
      <h2>Cấu hình hệ thống</h2>
      <p className="muted">Chọn một key có sẵn hoặc nhập key thủ công để dùng cho cấu hình hệ thống.</p>

      <form onSubmit={submit} className="stack-form" style={{ display: "grid", gap: 12 }}>
        <div className="form-field">
          <span>Key có sẵn</span>
          <select value={DEFAULT_KEYS.some((item) => item.key === form.key) ? form.key : ""} onChange={(e) => applyDefaultKey(e.target.value)}>
            {DEFAULT_KEYS.map((item) => (
              <option key={item.key} value={item.key}>{item.label} ({item.key})</option>
            ))}
            <option value="">Tự nhập key khác...</option>
          </select>
        </div>

        <div className="form-field">
          <span>Key</span>
          <input
            placeholder="smtp.host"
            value={form.key}
            onChange={(e) => setForm({ ...form, key: e.target.value })}
          />
        </div>

        <div className="form-field">
          <span>Value</span>
          <input
            placeholder="Giá trị cấu hình"
            value={form.value}
            onChange={(e) => setForm({ ...form, value: e.target.value })}
          />
        </div>

        <div className="form-field">
          <span>Description</span>
          <input
            placeholder="Mô tả"
            value={form.description}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
          />
        </div>

        <button className="btn-primary">Lưu</button>
      </form>

      {selectedDefault && (
        <p className="muted" style={{ marginTop: 12 }}>
          Đang chọn key mặc định: <strong>{selectedDefault.label}</strong>
        </p>
      )}

      <ul>
        {items.map((x) => (
          <li key={x.key}>
            {x.key}: {x.value}
          </li>
        ))}
      </ul>
    </section>
  );
}
