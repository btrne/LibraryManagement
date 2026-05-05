# 📚 Library Management API (Hệ thống Quản lý Thư viện)

[![.NET](https://img.shields.io/badge/.NET-10.0-512bd4)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/Database-SQL_Server-red)](https://www.microsoft.com/en-us/sql-server/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Một dự án RESTful API được xây dựng nhằm giải quyết bài toán quản lý thư viện thực tế. Dự án không chỉ dừng lại ở các thao tác CRUD cơ bản mà còn tập trung vào việc áp dụng các quy chuẩn công nghiệp (Professional Industry Standards), tối ưu hóa hiệu suất và đảm bảo tính bảo mật cao.

---

## 🚀 Tính năng nổi bật (Key Features)

### 🔹 Nghiệp vụ mượn trả nâng cao
- **Giao dịch mượn linh hoạt:** Hỗ trợ một độc giả mượn đồng thời từ 1 đến 5 cuốn sách trong một lần giao dịch.
- **Xử lý trả sách thông minh:** Cho phép trả lẻ từng cuốn hoặc trả toàn bộ danh sách đang mượn. Hệ thống tự động gán thời gian trả thực tế (`DateTime.Now`) và cập nhật trạng thái sách.
- **Quản lý thời hạn:** Tự động tính toán ngày hết hạn (`DueDate`), hỗ trợ lọc và xử lý các trường hợp quá hạn.

### 🔹 Kiến trúc & Kỹ thuật chuyên sâu
- **Mô hình DTO & AutoMapper:** Tách biệt hoàn toàn lớp Entity (Database) và lớp Dữ liệu trả về (Presentation). Điều này giúp bảo mật cấu trúc bảng và ngăn chặn triệt để lỗi tham chiếu vòng (Object Cycle).
- **Ràng buộc dữ liệu (Fluent API):** Thiết lập các chốt chặn dữ liệu chặt chẽ ngay từ cấp Database (ví dụ: Tiêu đề sách là duy nhất - Unique Index).
- **Phân trang & Tìm kiếm:** Tối ưu hóa hiệu suất bằng cách giới hạn dữ liệu trả về và hỗ trợ lọc sách theo tên, tác giả, thể loại.
- **Bảo mật JWT:** Tích hợp JSON Web Token để xác thực người dùng, bảo vệ các Endpoint nhạy cảm.
- **Xử lý lỗi tập trung:** Sử dụng Global Exception Middleware để đảm bảo API luôn phản hồi về định dạng JSON chuẩn ngay cả khi có sự cố hệ thống.

---

## 🛠 Công nghệ sử dụng (Tech Stack)

- **Backend Framework:** ASP.NET Core Web API (.NET 10)
- **Database:** Microsoft SQL Server (vận hành qua Docker)
- **ORM:** Entity Framework Core (Code-First Approach)
- **Mapping:** AutoMapper
- **API Documentation:** Scalar (Giao diện hiện đại, thay thế cho Swagger trên .NET 10)
- **Environment:** Phát triển và vận hành nhất quán trên WSL 2 (Ubuntu).

---

## 📂 API Endpoints tiêu biểu

| Method | Endpoint | Mô tả | Authentication |
|---|---|---|---|
| POST | `/api/auth/login` | Đăng nhập và lấy Bearer Token | ❌ |
| GET | `/api/books` | Danh sách sách (Phân trang & Lọc) | ✅ |
| POST | `/api/books/upload-image` | Tải lên ảnh bìa cho sách | ✅ |
| POST | `/api/borrowrecords` | Tạo phiếu mượn (1-5 cuốn) | ✅ |
| PUT | `/api/borrowrecords/{id}/return` | Xử lý trả sách theo ID phiếu | ✅ |
| GET | `/api/statistics/summary` | Lấy số liệu thống kê Dashboard | ✅ |

---

## ⚙️ Hướng dẫn Cài đặt & Chạy dự án (Getting Started)

### 1. Yêu cầu hệ thống
- .NET 10 SDK
- Docker Desktop (để chạy SQL Server)
- WSL 2 (Khuyến nghị cho người dùng Windows)

### 2. Các bước triển khai nhanh

```bash
# 1. Clone dự án về máy
git clone https://github.com/btrne/LibraryManagement.git
cd LibraryManagement

# 2. Khởi động SQL Server qua Docker
# Đảm bảo Connection String trong appsettings.Development.json đã chính xác

# 3. Cập nhật Cơ sở dữ liệu (Database Migration)
dotnet ef database update

# 4. Chạy ứng dụng
dotnet run
```

Truy cập giao diện quản lý API: **http://localhost:5188/scalar/v1**

---

## 📸 Ảnh chụp màn hình (Screenshots)

| Giao diện Tổng quan API (Scalar UI) | Xác thực người dùng (JWT Token) |
|:---:|:---:|
| <img width="1914" height="910" alt="Screenshot 2026-05-05 192956" src="https://github.com/user-attachments/assets/06a3d6e9-7227-42f3-bf1a-5e264bef7234" /> | <img width="1304" height="544" alt="Screenshot 2026-05-05 184607" src="https://github.com/user-attachments/assets/998f6eb7-a6d3-4544-a960-5e771f25cffa" /> |
| **Quản lý Đầu sách & Mã vạch (DTO)** | **Cấu trúc dữ liệu Phiếu mượn (Nested DTO)** |
| <img width="1295" height="798" alt="Screenshot 2026-05-05 193106" src="https://github.com/user-attachments/assets/34a53843-da45-489f-a7c9-a9544bbf9936" /> | <img width="1296" height="791" alt="Screenshot 2026-05-05 193457" src="https://github.com/user-attachments/assets/c8cf83be-ea7a-42c5-a0de-bfb38130e163" /> |

---

## 📈 Định hướng phát triển (Roadmap)
- [ ] Tích hợp Serilog để quản lý nhật ký hệ thống (Logging).
- [ ] Xây dựng Frontend bằng React.js để hoàn thiện hệ thống Fullstack.
---

## 👤 Tác giả

- **Nguyễn Thị Bích Trâm** - Backend Developer
- GitHub: [@btrne](https://github.com/btrne)
- Email: tram.nguyenthibich05@gmail.com
