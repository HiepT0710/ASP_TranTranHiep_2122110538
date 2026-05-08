import { useEffect, useState } from "react";
import { getAdminSettings, upsertAdminSetting } from "../../services/apiService";
import { useToast } from "../../context/ToastContext";
import { formatDateTime } from "../../utils/dateTime";

const DEFAULT_SETTINGS = [
  { key: "Shipping:DefaultFee", label: "Phí ship mặc định", description: "Phí giao hàng áp dụng khi không có cấu hình riêng.", value: "20000" },
  { key: "Shipping:FreeShipThreshold", label: "Ngưỡng free ship", description: "Giá trị đơn hàng tối thiểu để miễn phí ship.", value: "100000" },
  { key: "Order:CancelWindowMinutes", label: "Thời gian hủy đơn", description: "Số phút cho phép khách hủy đơn sau khi đặt.", value: "10" },
];

export default function AdminSettingsPage() {
  const { pushToast } = useToast();
  const [items, setItems] = useState(DEFAULT_SETTINGS);

  const loadData = async () => {
    try {
      const data = await getAdminSettings();
      const remoteItems = data.items || [];
      setItems(DEFAULT_SETTINGS.map((setting) => {
        const found = remoteItems.find((x) => x.key === setting.key);
        return found ? { ...setting, value: found.value ?? setting.value, description: found.description || setting.description } : setting;
      }));
    } catch {
      setItems(DEFAULT_SETTINGS);
    }
  };

  useEffect(() => { loadData(); }, []);

  const saveSetting = async (key, value, description) => {
    try {
      await upsertAdminSetting({ key, value, description });
      pushToast("Đã lưu cấu hình", "success");
      loadData();
    } catch (error) {
      pushToast(error?.response?.data?.message || "Không lưu được cấu hình", "error");
    }
  };

  return (
    <section className="page">
      <p className="eyebrow">Cấu hình hệ thống</p>
      <h2>Admin - Settings</h2>
      <div className="panel" style={{ marginBottom: 16 }}>
        <h3>Thiết lập nhanh</h3>
        {DEFAULT_SETTINGS.map((setting) => {
          const current = items.find((x) => x.key === setting.key) || setting;
          return (
            <div key={setting.key} className="panel" style={{ marginTop: 12 }}>
              <strong>{setting.label}</strong>
              <p className="muted">{setting.description}</p>
              <div className="row" style={{ gap: 12, flexWrap: "wrap" }}>
                <input style={{ minWidth: 240 }} value={current.value || ""} onChange={(e) => setItems((prev) => prev.map((x) => x.key === setting.key ? { ...x, value: e.target.value } : x))} />
                <button onClick={() => saveSetting(setting.key, current.value || setting.value, setting.description)}>Lưu</button>
              </div>
            </div>
          );
        })}
      </div>

      <div className="panel">
        <h3>Danh sách cấu hình</h3>
        <table className="table">
          <thead><tr><th>Key</th><th>Value</th><th>Mô tả</th><th>Cập nhật</th></tr></thead>
          <tbody>
            {items.map((item) => (
              <tr key={item.key}>
                <td>{item.key}</td>
                <td>{item.value}</td>
                <td>{item.description}</td>
                <td>{formatDateTime(item.updatedAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
