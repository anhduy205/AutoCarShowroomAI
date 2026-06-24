# Auto Car Showroom & AI Chatbot Consultant

Một hệ thống quản lý showroom ô tô toàn diện được phát triển trên nền tảng **ASP.NET Core MVC**. Dự án tích hợp trí tuệ nhân tạo (AI Chatbot) để tư vấn khách hàng, tự động hóa quy trình chăm sóc khách hàng qua Email SMTP, và áp dụng các tiêu chuẩn bảo mật tối ưu trong phát triển phần mềm.

---

## 🚀 Tính năng nổi bật

### 1. Hệ thống Quản trị & Phân quyền (RBAC)
* **Phân quyền chặt chẽ:** Phân chia rõ ràng vai trò giữa Quản trị viên (`Administrator`) và Nhân viên (`Staff`).
* **Quản lý nghiệp vụ:** Quản lý danh mục hãng xe, thông tin chi tiết xe, và xử lý đơn hàng chuyên nghiệp.
* **Audit Logs (Nhật ký hệ thống):** Ghi vết tự động toàn bộ thao tác của đội ngũ quản trị, đảm bảo tính minh bạch và bảo mật dữ liệu.
* **Dashboard trực quan:** Thống kê số lượng tồn kho, các dòng xe bán chạy kèm bộ lọc thời gian dynamic.

### 2. Tương tác & Tự động hóa Khách hàng
* **Tiếp nhận yêu cầu thông minh:** Hỗ trợ khách hàng gửi yêu cầu đặt lịch xem xe, đặt lịch lái thử, yêu cầu tư vấn hoặc đặt cọc trước.
* **Tự động hóa Email:** Hệ thống tự động gửi email thông báo qua giao thức SMTP (Gmail App Password) ngay khi Admin/Staff xác nhận trạng thái yêu cầu của khách.

### 3. Tích hợp Trí tuệ Nhân tạo (AI Chatbot)
* **Tư vấn 24/7:** Tích hợp trực tiếp với **Cloudflare Workers AI** (sử dụng Large Language Model `Llama 3.1 8b Instruct`) để hỗ trợ phản hồi, tư vấn thông tin xe cho khách hàng theo thời gian thực.

---

## 🛡️ Điểm nhấn Kỹ thuật & Bảo mật (Security & Best Practices)

Dự án được thiết kế hướng tới môi trường Production thực tế với các giải pháp bảo mật:
* **Mã hóa mật khẩu:** Sử dụng thuật toán băm mật khẩu nâng cao **PBKDF2-SHA256**, cam kết không lưu text thô trong cơ sở dữ liệu.
* **Bảo mật thông tin cấu hình:** Ứng dụng **User Secrets** trong môi trường Development nhằm ngăn chặn tuyệt đối việc rò rỉ các thông tin nhạy cảm (Connection Strings, SMTP Password, AI API Token) lên GitHub.
* **Phòng chống Brute-Force:** Tích hợp cơ chế **Rate Limiting theo IP** tại endpoint đăng nhập, tự động tạm khóa tài khoản khi phát hiện hành vi đăng nhập sai liên tiếp.

---

## 🛠️ Công nghệ sử dụng (Tech Stack)

* **Backend:** .NET 8.0 SDK, ASP.NET Core MVC
* **Database:** SQL Server / SQL Server Express
* **AI Integration:** Cloudflare Workers AI API (`@cf/meta/llama-3.1-8b-instruct`)
* **Email Service:** SMTP Client (Mã hóa SSL/TLS)
* **Tools:** SQL Server Management Studio (SSMS), PowerShell, .NET CLI

---

## 💻 Hướng dẫn Cài đặt & Khởi chạy

### 1. Yêu cầu hệ thống
* .NET SDK 8.0 trở lên
* SQL Server hoặc SQL Server Express
* Tiện ích dòng lệnh `sqlcmd` hoặc phần mềm SSMS

### 2. Sao chép mã nguồn và Build dự án
```bash
# Clone repository
git clone [https://github.com/anhduy205/AutoCarShowroomAI.git](https://github.com/anhduy205/AutoCarShowroomAI.git)
cd AutoCarShowroomAI

# Restore các packages và Build Solution
dotnet restore .\AutoCarShowRoomChatboxAI.sln
dotnet build .\AutoCarShowRoomChatboxAI.sln


3. Cấu hình Cơ sở dữ liệu (Database)
Mở file Showroom.Web/appsettings.json và cập nhật lại chuỗi kết nối (ConnectionStrings) phù hợp với cấu hình SQL Server nội bộ của bạn:

JSON
"ConnectionStrings": {
  "ShowroomDb": "Server=.\\SQLEXPRESS;Database=AutoCarShowroomDb;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;"
}
Trường hợp 1: Tạo mới Database từ đầu (Hệ thống mới / Khởi tạo dữ liệu mẫu)
Lưu ý: Lệnh này sẽ drop các bảng cũ nếu đã tồn tại và nạp lại toàn bộ schema cùng dữ liệu mẫu (Brands, Cars, Orders, AuditLogs,...).

PowerShell
sqlcmd -S ".\SQLEXPRESS" -E -i database\setup.sql
sqlcmd -S ".\SQLEXPRESS" -E -f 65001 -i database\seed-media.sql
Trường hợp 2: Cập nhật Database hiện có (Migration/Upgrade)
Dùng khi bạn muốn cập nhật schema mới mà không làm mất dữ liệu hiện tại:

PowerShell
sqlcmd -S ".\SQLEXPRESS" -E -i database\upgrade.sql
sqlcmd -S ".\SQLEXPRESS" -E -f 65001 -i database\seed-media.sql
🔑 Cấu hình Secrets bảo mật (Môi trường Local)
Để bảo mật các thông tin nhạy cảm, dự án sử dụng công cụ user-secrets. Hãy đứng tại thư mục gốc của dự án trên Terminal và chạy các lệnh mẫu sau:

1. Khởi tạo tài khoản Quản trị viên (Admin)
Chạy script PowerShell để tạo mã băm mật khẩu an toàn:

PowerShell
.\generate-admin-password-hash.ps1 -Password "Admin@123"
Sau đó, nạp thông tin đăng nhập kèm chuỗi băm vừa nhận được vào user-secrets:

Bash
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:Username" "admin"
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:PasswordHash" "AQAAAAIAAYagAAAAEOvX88sF2JdfXz99kLpQmW2kL819aaX..."
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:DisplayName" "Quản trị viên"
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:Role" "Administrator"
2. Cấu hình SMTP Email (Ví dụ kết nối Gmail)
Bash
dotnet user-secrets set --project .\Showroom.Web "Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set --project .\Showroom.Web "Smtp:Port" "587"
dotnet user-secrets set --project .\Showroom.Web "Smtp:EnableSsl" "true"
dotnet user-secrets set --project .\Showroom.Web "Smtp:Username" "showroom.demo.project@gmail.com"
dotnet user-secrets set --project .\Showroom.Web "Smtp:Password" "abcd efgh ijkl mnop"
dotnet user-secrets set --project .\Showroom.Web "Smtp:FromEmail" "showroom.demo.project@gmail.com"
dotnet user-secrets set --project .\Showroom.Web "Smtp:FromName" "Auto Car Showroom Service"
3. Cấu hình Kết nối API Cloudflare Workers AI
Bash
dotnet user-secrets set --project .\Showroom.Web "Ai:CloudflareWorkersAi:AccountId" "8b93600326abcdef1234567890abcdef"
dotnet user-secrets set --project .\Showroom.Web "Ai:CloudflareWorkersAi:ApiToken" "CloudflareToken_AbCdEfGhIjKlMnOpQrStUvW12345"
dotnet user-secrets set --project .\Showroom.Web "Ai:CloudflareWorkersAi:Model" "@cf/meta/llama-3.1-8b-instruct"
💡 Chú thích: Hướng dẫn chi tiết cách lấy thông tin cấu hình
[!TIP]

📑 Cách lấy Mật khẩu ứng dụng Gmail (Smtp:Password)
Truy cập vào tài khoản Google cá nhân > Chọn mục Bảo mật (Security).

Kích hoạt tính năng Xác minh 2 bước (2-Step Verification) nếu chưa bật.

Sau khi bật xong, kéo xuống dưới cùng tìm mục Mật khẩu ứng dụng (App Passwords).

Tạo một tên gợi nhớ ứng dụng (Ví dụ: AutoCarShowroom), hệ thống sẽ cấp một chuỗi mật khẩu gồm 16 ký tự ngẫu nhiên. Hãy sao chép chuỗi này và điền vào trường Smtp:Password.

[!TIP]

📑 Cách lấy Account ID và API Token từ Cloudflare
Đăng nhập vào Dashboard của Cloudflare.

Tại trang chủ Dashboard, nhìn sang thanh menu bên phải để copy chuỗi mã hóa tại mục Account ID.

Để tạo Token, truy cập menu AI > Workers AI > Chọn Manage API Tokens.

Tiến hành tạo một Token mới cấp quyền đọc/gọi các mô hình AI, sau đó copy chuỗi mã nhận được để cấu hình cho Ai:CloudflareWorkersAi:ApiToken.

🏃 Khởi chạy Ứng dụng
Chạy lệnh dưới đây để khởi động server local:

Bash
dotnet run --project .\Showroom.Web
Mặc định ứng dụng sẽ lắng nghe tại các cổng:

HTTP: http://localhost:5099

HTTPS: https://localhost:7299

Luồng kiểm tra nhanh (Quick Test Workflow):

Truy cập /cars để xem danh mục sản phẩm xe.

Truy cập /requests/create thực hiện gửi một yêu cầu demo (Đặt lịch/Tư vấn).

Đăng nhập vào trang quản trị với tài khoản Admin (admin / Admin@123).

Điều hướng tới danh sách Yêu cầu khách hàng và bấm Xác nhận để kiểm tra tính năng tự động gửi mail.

🧪 Kiểm thử (Testing)
Dự án hỗ trợ chạy integration test tự động cho database và logic nghiệp vụ:

Bash
dotnet build .\AutoCarShowRoomChatboxAI.sln
dotnet test .\AutoCarShowRoomChatboxAI.sln
