import { useState } from "react";
import { createModerationReport } from "../services/apiService";
import { useToast } from "../context/ToastContext";

const REASONS = [
  { id: "spam", label: "Spam / quảng cáo" },
  { id: "abuse", label: "Lăng mạ / xúc phạm" },
  { id: "fake", label: "Thông tin giả / sai sự thật" },
  { id: "offensive", label: "Nội dung phản cảm" },
  { id: "other", label: "Lý do khác" },
];

export default function ReportButton({ targetType, targetId, label = "Báo cáo" }) {
  const { pushToast } = useToast();
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState(REASONS[0].id);
  const [detail, setDetail] = useState("");

  const submit = async () => {
    try {
      await createModerationReport({ targetType, targetId, reason, detail });
      pushToast("Đã gửi báo cáo", "success");
      setOpen(false);
      setDetail("");
    } catch (error) {
      pushToast(error?.response?.data?.message || "Không gửi được báo cáo", "error");
    }
  };

  return (
    <>
      <button type="button" className="secondary" onClick={() => setOpen(true)}>{label}</button>
      {open && (
        <div className="panel" style={{ marginTop: 12 }}>
          <h4 style={{ marginTop: 0 }}>Gửi báo cáo</h4>
          <select value={reason} onChange={(e) => setReason(e.target.value)}>
            {REASONS.map((r) => <option key={r.id} value={r.id}>{r.label}</option>)}
          </select>
          <textarea rows={4} value={detail} onChange={(e) => setDetail(e.target.value)} placeholder="Mô tả thêm (không bắt buộc)" style={{ marginTop: 10 }} />
          <div className="row" style={{ marginTop: 10 }}>
            <button type="button" onClick={submit}>Gửi</button>
            <button type="button" className="secondary" onClick={() => setOpen(false)}>Hủy</button>
          </div>
        </div>
      )}
    </>
  );
}
