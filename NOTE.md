# 📌 SavageExpenseTracker - Danh Sách Việc Cần Làm (TODO List)

---

### 🤖 1. Phân hệ Trợ lý ảo AI (Savage AI Assistant)
- [ ] **Tích hợp Gemini API**: Viết `GeminiService` kết nối đến Google AI Studio REST API trong .NET 8.
- [ ] **Prompt Engineering**: Thiết lập System Prompt định hình tính cách *"Bé Slime mỏ hỗn nhưng dễ thương"*.
- [ ] **Background Job (Async Queue)**: Tạo `Channel<long>` / `IHostedService` sinh câu khịa ngầm sau khi lưu khoản chi tiêu để tránh làm nghẽn request người dùng.
- [ ] **Cập nhật Savage Comment**: Tự động lưu kết quả câu khịa vào cột `SavageComment` trong CSDL.

---

### 📊 2. Phân hệ Thống kê & Biểu đồ (Analytics & Dashboard)
- [ ] **Endpoint Dashboard (`GET /api/analytics/dashboard`)**:
  - Tính tổng chi tiêu Hôm nay, Tuần này, Tháng này (hiển thị song song VNĐ và Số giờ công quy đổi).
  - Lấy câu khịa AI mới nhất.
- [ ] **Endpoint Lịch chi tiêu (`GET /api/analytics/calendar`)**:
  - Trả về tổng chi tiêu theo từng ngày trong tháng để vẽ Heatmap (mức độ tiêu hoang).
- [ ] **Endpoint Biểu đồ Danh mục (`GET /api/analytics/categories`)**:
  - Thống kê tỷ lệ % chi tiêu theo từng Danh mục (Ăn uống, Mua sắm...) để vẽ Biểu đồ Tròn (Pie Chart).

---

### 🖼️ 3. Tải & Nén Tối ưu Ảnh Hóa đơn (Image Processing)
- [x] **Cloudinary Photo Service**: Nén và tối ưu hóa ảnh tự động bằng Cloudinary API (`CloudinaryPhotoService.cs`).
- [x] **Endpoint Upload Ảnh (`POST /api/expenses/upload-photo`)**: Nhận ảnh hóa đơn và trả về link CDN an toàn.

---

### ⚔️ 4. Nâng cấp Phân hệ Đấu trường (Challenges v2.0)
- [ ] **Fog of War (Sương mù chiến tranh)**: Ẩn thông tin Bảng xếp hạng sau 18:00 ngày Chủ Nhật để tạo kịch tính chốt giải.
- [ ] **SignalR Realtime Leaderboard Jump**: Khi ai đó thêm Expense mới, tự động bắn sự kiện SignalR nhảy số Bảng xếp hạng trực tiếp trên giao diện của các thành viên trong phòng.
- [ ] **Redis Sorted Sets (Tùy chọn)**: Lưu Bảng xếp hạng trên RAM Redis để tối ưu tốc độ đọc.