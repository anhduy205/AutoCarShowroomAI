# Auto Car Showroom Chatbox AI

Ứng dụng ASP.NET Core MVC để quản lý showroom ô tô, bao gồm:

- Đăng nhập khu vực quản trị với phân quyền `Administrator` va `Staff`
- Quản lý hãng xe, xe, đơn hàng
- Tiếp nhận yêu cầu khách hàng: đặt lịch xem xe, tư vấn, đặt cọc trước, lịch lái thử
- Admin xác nhận yêu cầu và gửi email SMTP cho khách hàng
- Nhật ký thao tác quản trị
- Dashboard thống kê tồn kho và xe bán chạy có bộ lọc thời gian
- Chatbot AI tư vấn xe

## Yêu cầu cài đặt

- .NET SDK 8.0 hoặc moi hon
- SQL Server hoặc SQL Server Express
- SQL Server Management Studio (SSMS) hoặc `sqlcmd`
- PowerShell tren Windows

## Cài đặt sau khi clone từ GitHub

Clone repo:

```powershell
git clone <REPO_URL>
cd AutoCarShowroomAI
```

Restore package va build:

```powershell
dotnet restore .\AutoCarShowRoomChatboxAI.sln
dotnet build .\AutoCarShowRoomChatboxAI.sln
```

## Cấu hình database

Mở file [Showroom.Web/appsettings.json](Showroom.Web/appsettings.json) và sửa connection string nếu SQL Server của bạn khác:

```json
"ConnectionStrings": {
  "ShowroomDb": "Server=.\\SQLEXPRESS;Database=AutoCarShowroomDb;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;"
}
```

### Tạo mới database

Dùng khi máy mới clone về chưa có database, hoặc muốn tạo lại từ đầu. Lệnh này sẽ xóa các bảng cũ trong `AutoCarShowroomDb` và nạp dữ liệu mẫu.

Bảng được tạo gồm: `Brands`, `Cars`, `Orders`, `OrderItems`, `CustomerRequests`, `StaffUsers`, `AuditLogs`.

Chay bang `sqlcmd`:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -i database\setup.sql
sqlcmd -S ".\SQLEXPRESS" -E -f 65001 -i database\seed-media.sql
```

Nếu SQL Server của bạn là instance khác, thay `.\SQLEXPRESS` bằng server trong connection string, ví dụ:

```powershell
sqlcmd -S "DESKTOP-F0H15PT\SQLEXPRESS" -E -i database\setup.sql
```

Hoặc mở `database/setup.sql` trong SSMS và bấm Execute.

### Cập nhật database đang có

Dùng khi đã có database và chỉ muốn bổ sung bảng/cột/ràng buộc mới, không tạo lại toàn bộ database:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -i database\upgrade.sql
sqlcmd -S ".\SQLEXPRESS" -E -f 65001 -i database\seed-media.sql
```

Hoặc mở `database/upgrade.sql` trong SSMS và bấm Execute.

Ghi chú: `setup.sql` phù hợp cho môi trường dev/demo vì có xóa bảng cũ. `upgrade.sql` phù hợp hơn khi muốn giữ dữ liệu hiện có.

## Cấu hình tài khoản quản trị

Repo không nên lưu password thật trong code. Hãy dùng `user-secrets` hoặc biến môi trường.

Tao password hash:

```powershell
.\generate-admin-password-hash.ps1 -Password "Admin@123"
```

Gan tai khoan admin bang `user-secrets`:

```powershell
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:Username" "admin"
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:PasswordHash" "<PASTE_HASH_HERE>"
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:DisplayName" "Quản trị viên"
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:Role" "Administrator"
```

Tài khoản nhân viên có thể thêm tương tự với index `1` va role `Staff`.

## Cấu hình SMTP để gửi email thật

Hệ thống gửi email khi admin xác nhận yêu cầu khách hàng. Nên cấu hình SMTP bằng `user-secrets`, không nên commit mật khẩu vào GitHub.

Vi du Gmail:

```powershell
dotnet user-secrets set --project .\Showroom.Web "Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set --project .\Showroom.Web "Smtp:Port" "587"
dotnet user-secrets set --project .\Showroom.Web "Smtp:EnableSsl" "true"
dotnet user-secrets set --project .\Showroom.Web "Smtp:Username" "your-email@gmail.com"
dotnet user-secrets set --project .\Showroom.Web "Smtp:Password" "your-gmail-app-password"
dotnet user-secrets set --project .\Showroom.Web "Smtp:FromEmail" "your-email@gmail.com"
dotnet user-secrets set --project .\Showroom.Web "Smtp:FromName" "Auto Car Showroom"
```

Với Gmail, `Smtp:Password` phải là App Password 16 ký tự, không phải mật khẩu đăng nhập Gmail thường. Tài khoản Gmail phải bật 2-Step Verification.

Nếu chưa cấu hình SMTP đầy đủ, khi admin xác nhận yêu cầu khách hàng app sẽ báo:

```text
Chưa cấu hình SMTP nên chưa thể gửi email thật.
```

## Cau hinh chatbot AI

Lay `Account ID` va tao `Workers AI API Token` trong Cloudflare. Sau do set secrets:

```powershell
dotnet user-secrets set --project .\Showroom.Web "Ai:CloudflareWorkersAi:AccountId" "<CLOUDFLARE_ACCOUNT_ID>"
dotnet user-secrets set --project .\Showroom.Web "Ai:CloudflareWorkersAi:ApiToken" "<CLOUDFLARE_API_TOKEN>"
dotnet user-secrets set --project .\Showroom.Web "Ai:CloudflareWorkersAi:Model" "@cf/meta/llama-3.1-8b-instruct"
```

## Chay ung dung

```powershell
dotnet run --project .\Showroom.Web
```

Mac dinh app se chay theo launch settings, thuong la:

- `http://localhost:5099`
- `https://localhost:7299`

Một luồng kiểm tra nhanh:

1. Vào `/cars` để xem danh sách xe.
2. Vào `/requests/create` để gửi yêu cầu khách hàng.
3. Đăng nhập admin.
4. Vào `Yêu cầu khách`.
5. Bấm `Xác nhận` để gửi email cho khách.

## Test

```powershell
dotnet build .\AutoCarShowRoomChatboxAI.sln
dotnet test .\AutoCarShowRoomChatboxAI.sln
```

Test SQL integration se dung `ShowroomDb` trong `Showroom.Web/appsettings.json`, hoặc bien moi truong:

```powershell
$env:SHOWROOM_TEST_SQL_CONNECTION_STRING = "Server=...;Database=master;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;"
```

Test helper sẽ tự tạo database tạm và tự động xóa sau khi test xong.

## Lỗi thường gặp

### Không gửi được email

- Kiểm tra `Smtp:Host`, `Smtp:Port`, `Smtp:Username`, `Smtp:Password`, `Smtp:FromEmail`
- Gmail phải dùng App Password
- Khách hàng phải có email trong yêu cầu

### Thiếu bảng hoặc cột database

Chay:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -i database\upgrade.sql
```

### SQL Server không kết nối được

- Kiểm tra instance SQL Server đang chạy
- Kiểm tra `ConnectionStrings:ShowroomDb`
- Nếu dùng SQL auth, đổi connection string sang dạng `User Id=...;Password=...;`

## Ghi chú bảo mật

- Không commit SMTP password, Cloudflare token, password hash thật lên GitHub
- Nên dùng `user-secrets` cho môi trường local
- Password hash dung `pbkdf2-sha256`
- Login POST co rate limiting theo IP
- Tài khoản bị tạm khóa sau nhiều lần đăng nhập sai liên tiếp
