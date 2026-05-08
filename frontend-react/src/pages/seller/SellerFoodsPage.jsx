import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import ActionMenu from "../../components/ActionMenu";
import { deleteSellerFood, getSellerFoods, updateSellerFoodFlags } from "../../services/apiService";
import { useToast } from "../../context/ToastContext";

export default function SellerFoodsPage() {
  const { pushToast } = useToast();
  const [data, setData] = useState({ items: [], page: 1, total: 0, pageSize: 10 });
  const [filter, setFilter] = useState({ page: 1, pageSize: 10 });
  const [msg, setMsg] = useState("");

  const loadData = async () => {
    const res = await getSellerFoods(filter);
    setData(res);
  };

  useEffect(() => {
    loadData();
  }, [filter.page, filter.pageSize]);

  const updateFlags = async (id, payload, successMsg) => {
    try {
      await updateSellerFoodFlags(id, payload);
      setMsg(successMsg);
      pushToast(successMsg, "success");
      loadData();
    } catch (error) {
      const message = error?.response?.data?.message || "Không cập nhật được món";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  const remove = async (id) => {
    try {
      await deleteSellerFood(id);
      const message = "Đã xóa món";
      setMsg(message);
      pushToast(message, "success");
      loadData();
    } catch (error) {
      const message = error?.response?.data?.message || "Không xóa được món";
      setMsg(message);
      pushToast(message, "error");
    }
  };

  return (
    <section className="page">
      <div className="row" style={{ justifyContent: "space-between" }}>
        <div>
          <p className="eyebrow">Seller workspace</p>
          <h2>Quản lý món ăn của quán</h2>
        </div>
        <div className="row">
          <select value={filter.pageSize} onChange={(e) => setFilter({ ...filter, pageSize: Number(e.target.value), page: 1 })}>
            <option value={10}>10</option>
            <option value={20}>20</option>
            <option value={50}>50</option>
          </select>
          <Link to="/seller/foods/new">+ Thêm món</Link>
        </div>
      </div>
      {msg && <p className="ok">{msg}</p>}
      <table className="table">
        <thead><tr><th>ID</th><th>Tên</th><th>Danh mục</th><th>Giá</th><th>Tồn kho</th><th>Trạng thái</th><th>Actions</th></tr></thead>
        <tbody>
          {(data.items || []).map((x) => (
            <tr key={x.id}>
              <td>{x.id}</td>
              <td>{x.name}</td>
              <td>{x.categoryName}</td>
              <td>{x.price}</td>
              <td>{x.stockQuantity}</td>
              <td>
                <div className="row" style={{ flexWrap: "wrap" }}>
                  <span className={`badge ${x.isAvailable ? "success" : "warning"}`}>{x.isAvailable ? "Đang bán" : "Ngừng bán"}</span>
                  {x.isHidden && <span className="badge">Đang ẩn</span>}
                </div>
              </td>
              <td>
                <ActionMenu
                  label="Thao tác"
                  items={[
                    { label: x.isAvailable ? "Tạm dừng bán" : "Mở bán lại", onClick: () => updateFlags(x.id, { isAvailable: !x.isAvailable }, "Đã cập nhật trạng thái bán") },
                    { label: x.isHidden ? "Hiện món" : "Ẩn món", onClick: () => updateFlags(x.id, { isHidden: !x.isHidden }, "Đã cập nhật trạng thái hiển thị") },
                    { label: "Đặt hết hàng", onClick: () => updateFlags(x.id, { stockQuantity: 0 }, "Đã đặt món hết hàng") },
                    { label: "Sửa", onClick: () => window.location.assign(`/seller/foods/${x.id}/edit`) },
                    { label: "Xóa", onClick: () => remove(x.id), variant: "ghost" },
                  ]}
                />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <div className="row">
        <button disabled={filter.page <= 1} onClick={() => setFilter({ ...filter, page: filter.page - 1 })}>Prev</button>
        <span>Trang {data.page || filter.page}</span>
        <button onClick={() => setFilter({ ...filter, page: filter.page + 1 })}>Next</button>
      </div>
    </section>
  );
}
