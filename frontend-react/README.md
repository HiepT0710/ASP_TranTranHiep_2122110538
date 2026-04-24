# Frontend React cho FoodOrder API

## 1) Cài đặt

```bash
npm install
```

## 2) Cấu hình API URL

Tạo file `.env` trong thư mục `frontend-react`:

```env
VITE_API_BASE_URL=http://localhost:5208
```

Nếu backend chạy HTTPS thì đổi thành `https://localhost:7187`.

## 3) Chạy frontend

```bash
npm run dev
```

Mặc định: [http://localhost:5173](http://localhost:5173)

## 4) Chạy backend

Trong project ASP.NET Core:

```bash
dotnet run
```

## Chức năng frontend đã triển khai

- Public:
  - Trang chủ, danh sách quán, danh sách món, giỏ hàng.
- User:
  - Đăng ký user/seller, đăng nhập/đăng xuất, hồ sơ cá nhân.
  - Checkout, xem đơn của tôi, hủy đơn khi hợp lệ.
- Seller:
  - Dashboard thống kê quán.
  - Quản lý danh mục.
  - CRUD món ăn đầy đủ (create/edit/delete) có upload ảnh qua `FromForm`.
  - Quản lý đơn có filter + phân trang + cập nhật trạng thái.
  - Xem trang chi tiết đơn (status history, chat logs, review state).
- Admin:
  - Dashboard tổng quan hệ thống.
  - Quản lý users (đổi role).
  - Duyệt/từ chối/tạm ngưng quán.
  - CRUD món ăn toàn hệ thống (create/edit/delete) có upload ảnh qua `FromForm`.
  - Quản lý đơn hàng có filter + phân trang + cập nhật trạng thái.
  - Xem trang chi tiết đơn.

- User nâng cao:
  - Danh sách đơn có phân trang.
  - Trang chi tiết đơn gồm: món trong đơn, lịch sử trạng thái, chat message, gửi review món (nếu đủ điều kiện).
