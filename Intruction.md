# Bước 1: Tạo Entity ở tầng Domain (Tầng Lõi)

**File viết trước:**
- `Book.cs`

**Lý do:**

Đây là cốt lõi của tính năng. Bạn phải xác định được cuốn sách của mình gồm những thông tin gì (Id, Tên, Tác giả, Giá...) trước khi làm các việc khác.

---

# Bước 2: Tạo DTOs ở tầng Application (Tầng Thiết kế đầu vào/đầu ra)

**File tiếp theo:**
- `BookDto.cs`
- `CreateBookDto.cs`

**Lý do:**

Định hình xem API của bạn sẽ nhận vào cái gì và trả ra cái gì cho client.

---

# Bước 3: Định nghĩa bản hợp đồng dữ liệu (Interface Repository) ở tầng Application

**File tiếp theo:**
- `IBookRepository.cs`

**Lý do:**

Bạn tự hỏi:

> "Để làm tính năng này, tôi cần tương tác gì với database?"

(Cần lưu, cần xóa, cần tìm theo Id...)

Bạn liệt kê các hàm đó vào Interface này.

---

# Bước 4: Triển khai Database ở tầng Infrastructure (Tầng Điện nước)

**Các file tiếp theo:**
- Cấu hình bảng trong `BookStoreDbContext.cs`
- Viết code EF Core thực tế trong `BookRepository.cs` để thực thi bản hợp đồng `IBookRepository`

**Lý do:**

Có database và repository rồi thì mới có dữ liệu để viết logic nghiệp vụ.

---

# Bước 5: Viết Logic nghiệp vụ (Interface Service & Class Service) ở tầng Application

**Các file tiếp theo:**
- `IBookService.cs`
- `BookService.cs`

**Lý do:**

Đây là nơi kết nối dữ liệu từ Repository ở Bước 4, xử lý logic, rồi chuyển thành DTO để chuẩn bị trả về cho API.

---

# Bước 6: Tạo API Endpoint ở tầng WebApi (Tầng Mặt tiền)

**File tiếp theo:**
- `BooksController.cs`

**Lý do:**

Viết Controller để tiếp nhận HTTP Request (`GET`, `POST`, `PUT`, `DELETE`), gọi Service tương ứng ở Bước 5 để xử lý và trả kết quả về cho client.

---

# Bước 7: Đăng ký liên kết ở Program.cs (Bật công tắc nguồn)

**File cuối cùng:**
- `Program.cs`

**Lý do:**

Khai báo Dependency Injection (DI) để hệ thống biết liên kết:

- `IBookRepository` → `BookRepository`
- `IBookService` → `BookService`

Nếu không có bước này, khi chạy ứng dụng sẽ bị báo lỗi không tìm thấy dịch vụ tương ứng.