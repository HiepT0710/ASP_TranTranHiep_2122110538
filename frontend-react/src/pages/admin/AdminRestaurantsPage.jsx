import { useEffect, useState } from "react";
import ActionMenu from "../../components/ActionMenu";
import SortFilterBar from "../../components/SortFilterBar";
import { adminRestaurantAction, getAdminRestaurantDetails, getAdminRestaurants } from "../../services/apiService";
import { useToast } from "../../context/ToastContext";
import { formatDateTime } from "../../utils/dateTime";

export default function AdminRestaurantsPage() {
  const { pushToast } = useToast();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState("");
  const [detail, setDetail] = useState(null);
  const [meta, setMeta] = useState({ page: 1, total: 0, totalPages: 1 });
  const [filter, setFilter] = useState({ page: 1, pageSize: 50, q: "", sortBy: "name_asc" });

  const loadData = async (nextFilter = filter) => {
    setLoading(true);
    try {
      const data = await getAdminRestaurants(nextFilter);
      const total = data.total || 0;
      const totalPages = Math.max(1, Math.ceil(total / nextFilter.pageSize));
      const page = Math.min(data.page || nextFilter.page, totalPages);
      setItems(data.items || []);
      setMeta({ page, total, totalPages });
      setFilter((current) => (current.page > totalPages ? { ...current, page: totalPages } : current));
    } catch (error) {
      setMsg(error?.response?.data?.message || "Không tải được danh sách quán");
    } finally {
      setLoading(false);
    }
  };

  const loadDetail = async (id) => {
    try {
      const data = await getAdminRestaurantDetails(id);
      setDetail(data);
    } catch (error) {
      setDetail(null);
      setMsg(error?.response?.data?.message || "Không tải được chi tiết quán");
    }
  };

  useEffect(() => {
    loadData(filter);
  }, [filter.page, filter.pageSize, filter.q, filter.sortBy]);

  const action = async (id, name) => {
    try {
      await adminRestaurantAction(id, name);
      const message = name === "Approve" ? "Đã duyệt quán" : name === "Reject" ? "Đã từ chối quán" : "Đã tạm ngưng quán";
      setMsg(message);
      pushToast(message, "success");
      loadData();
    } catch (error) {
      const message = error?.response?.data?.message || "Không thể cập nhật quán";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  return (
    <section className="page">
      <div className="row" style={{ justifyContent: "space-between", alignItems: "center", flexWrap: "wrap" }}>
        <div>
          <p className="eyebrow">Quản trị quán</p>
          <h2>Admin - Duyệt quán</h2>
        </div>
        <div className="row">
          <select value={filter.pageSize} onChange={(e) => setFilter({ ...filter, pageSize: Number(e.target.value), page: 1 })}>
            <option value={20}>20</option>
            <option value={50}>50</option>
            <option value={100}>100</option>
          </select>
        </div>
      </div>
      <SortFilterBar
        value={filter}
        onChange={setFilter}
        filters={[
          { key: "q", label: "Từ khóa", type: "input", placeholder: "Tìm theo tên quán" },
        ]}
        sortOptions={[
          { value: "name_asc", label: "Tên A → Z" },
          { value: "name_desc", label: "Tên Z → A" },
          { value: "newest", label: "Mới nhất" },
          { value: "oldest", label: "Cũ nhất" },
        ]}
      />
      {msg && <p className="ok">{msg}</p>}
      {loading ? (
        <div className="panel">Đang tải quán...</div>
      ) : (
        <>
        <table className="table">
          <thead><tr><th>ID</th><th>Tên quán</th><th>Chủ quán</th><th>Trạng thái</th><th>Trạng thái cập nhật</th><th>Thao tác</th></tr></thead>
          <tbody>
            {items.map((r) => (
              <tr key={r.id}>
                <td>{r.id}</td>
                <td>{r.name}</td>
                <td>{r.ownerUsername}</td>
                <td><span className="badge">{r.status}</span></td>
                <td>{formatDateTime(r.statusUpdatedAt)}</td>
                <td>
                  <ActionMenu
                    label="Thao tác"
                    items={[
                      { label: "Xem chi tiết", onClick: () => loadDetail(r.id) },
                      { label: "Duyệt quán", onClick: () => action(r.id, "Approve") },
                      { label: "Từ chối", onClick: () => action(r.id, "Reject") },
                      { label: "Tạm ngưng", onClick: () => action(r.id, "Suspend"), variant: "ghost" },
                      { label: "Mở lại", onClick: () => action(r.id, "Reopen"), variant: "ghost" },
                    ]}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {detail && (
          <div className="panel" style={{ marginTop: 16 }}>
            <h3>Chi tiết quán #{detail.id}</h3>
            <p className="muted">Tên quán: <b>{detail.name}</b></p>
            <p className="muted">Chủ quán: <b>{detail.ownerUsername}</b></p>
            <p className="muted">Trạng thái: <b>{detail.status}</b></p>
            <p className="muted">Ghi chú: <b>{detail.statusNote || "N/A"}</b></p>
            <p className="muted">Cập nhật lúc: <b>{formatDateTime(detail.statusUpdatedAt)}</b></p>
          </div>
        )}
        {meta.totalPages > 1 && (
          <div className="row">
            <button className="secondary" disabled={filter.page <= 1} onClick={() => setFilter({ ...filter, page: filter.page - 1 })}>Prev</button>
            <span className="badge">Trang {meta.page} / {meta.totalPages}</span>
            <button className="secondary" disabled={filter.page >= meta.totalPages} onClick={() => setFilter({ ...filter, page: filter.page + 1 })}>Next</button>
          </div>
        )}
        </>
      )}
    </section>
  );
}
