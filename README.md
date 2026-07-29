# SavageExpenseTracker — Web API

Dự án Web API quản lý chi tiêu cá nhân kết hợp yếu tố thử thách tài chính (Savage Expense Tracker) xây dựng bằng **.NET 8** và **PostgreSQL**.

---

## 🛠️ Yêu cầu môi trường

- **.NET 8.0 SDK** trở lên
- **PostgreSQL 14+**
- Thư viện EF Core CLI Tools (Cài bằng lệnh: `dotnet tool install --global dotnet-ef` nếu chưa có)

---

## ⚙️ Cấu hình Môi trường (`.env`)

Tạo file `.env` ở thư mục gốc dự án (`d:\SavageExpenseTracker\.env`) với các biến môi trường sau:

```env
DB_CONNECTION_STRING
JWT_SECRET_KEY
JWT_ISSUER
JWT_AUDIENCE
```

---

## 📦 Hướng dẫn Khởi tạo & Cập nhật CSDL (EF Core Migrations)

### 1. Tạo file Migration (khi thay đổi Entity / DbContext):
```bash
dotnet ef migrations add InitialCreate --project src/SavageExpenseTracker.Infrastructure --startup-project src/SavageExpenseTracker.WebApi
```

### 2. Cập nhật / Dựng CSDL vào PostgreSQL (chạy trên máy mới hoặc máy hiện tại):
```bash
dotnet ef database update --project src/SavageExpenseTracker.Infrastructure --startup-project src/SavageExpenseTracker.WebApi
```

---

## 🚀 Hướng dẫn Chạy ứng dụng

Từ thư mục gốc dự án, chạy lệnh:

```bash
dotnet run --project src/SavageExpenseTracker.WebApi
```

---

## 🔑 Kiểm thử API trên Swagger UI & JWT Auth

1. Trình duyệt tự động mở hoặc truy cập đường dẫn Swagger (vd: `https://localhost:7021/swagger` hoặc `http://localhost:5269/swagger`).
2. Sử dụng API `POST /api/users/register` để đăng ký và `POST /api/users/login` để đăng nhập lấy `accessToken`.
3. Nhấp vào nút **Authorize** ở góc phải trên của Swagger UI.
4. Dán trực tiếp chuỗi `accessToken` vào ô Value và bấm **Authorize**.
5. Bây giờ bạn có thể trải nghiệm tất cả các API như `GET /api/expenses/me`, `GET /api/categories`,...
