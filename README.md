# Auto Car Showroom Chatbox AI

Ung dung ASP.NET Core MVC de quan ly showroom o to, bao gom:

- Dang nhap khu vuc quan tri voi phan quyen `Administrator` va `Staff`
- Quan ly hang xe, xe, don hang
- Tiep nhan yeu cau khach hang: dat lich xem xe, tu van, dat coc truoc, lich lai thu
- Admin xac nhan yeu cau va gui email SMTP cho khach hang
- Nhat ky thao tac quan tri
- Dashboard thong ke ton kho va xe ban chay co bo loc thoi gian
- Chatbot AI tu van xe

## Yeu cau cai dat

- .NET SDK 8.0 hoac moi hon
- SQL Server hoac SQL Server Express
- SQL Server Management Studio (SSMS) hoac `sqlcmd`
- PowerShell tren Windows

## Cai dat sau khi clone tu GitHub

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

## Cau hinh database

Mo file [Showroom.Web/appsettings.json](Showroom.Web/appsettings.json) va sua connection string neu SQL Server cua ban khac:

```json
"ConnectionStrings": {
  "ShowroomDb": "Server=.\\SQLEXPRESS;Database=AutoCarShowroomDb;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;"
}
```

### Tao moi database

Dung khi may moi clone ve chua co database, hoac muon tao lai tu dau. Lenh nay se xoa cac bang cu trong `AutoCarShowroomDb` va nap du lieu mau.

Bang duoc tao gom: `Brands`, `Cars`, `Orders`, `OrderItems`, `CustomerRequests`, `StaffUsers`, `AuditLogs`.

Chay bang `sqlcmd`:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -i database\setup.sql
```

Neu SQL Server cua ban la instance khac, thay `.\SQLEXPRESS` bang server trong connection string, vi du:

```powershell
sqlcmd -S "DESKTOP-F0H15PT\SQLEXPRESS" -E -i database\setup.sql
```

Hoac mo `database/setup.sql` trong SSMS va bam Execute.

### Cap nhat database dang co

Dung khi da co database va chi muon bo sung bang/cot/rang buoc moi, khong tao lai toan bo database:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -i database\upgrade.sql
```

Hoac mo `database/upgrade.sql` trong SSMS va bam Execute.

Ghi chu: `setup.sql` phu hop cho moi truong dev/demo vi co xoa bang cu. `upgrade.sql` phu hop hon khi muon giu du lieu hien co.

## Cau hinh tai khoan quan tri

Repo khong nen luu password that trong code. Hay dung `user-secrets` hoac bien moi truong.

Tao password hash:

```powershell
.\generate-admin-password-hash.ps1 -Password "Admin@123"
```

Gan tai khoan admin bang `user-secrets`:

```powershell
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:Username" "admin"
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:PasswordHash" "<PASTE_HASH_HERE>"
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:DisplayName" "Quan tri vien"
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:Role" "Administrator"
```

Tai khoan nhan vien co the them tuong tu voi index `1` va role `Staff`.

## Cau hinh SMTP de gui email that

He thong gui email khi admin xac nhan yeu cau khach hang. Nen cau hinh SMTP bang `user-secrets`, khong nen commit mat khau vao GitHub.

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

Voi Gmail, `Smtp:Password` phai la App Password 16 ky tu, khong phai mat khau dang nhap Gmail thuong. Tai khoan Gmail phai bat 2-Step Verification.

Neu chua cau hinh SMTP day du, khi admin xac nhan yeu cau khach hang app se bao:

```text
Chua cau hinh SMTP nen chua the gui email that.
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

Mot luong kiem tra nhanh:

1. Vao `/cars` de xem danh sach xe.
2. Vao `/requests/create` de gui yeu cau khach hang.
3. Dang nhap admin.
4. Vao `Yeu cau khach`.
5. Bam `Xac nhan` de gui email cho khach.

## Test

```powershell
dotnet build .\AutoCarShowRoomChatboxAI.sln
dotnet test .\AutoCarShowRoomChatboxAI.sln
```

Test SQL integration se dung `ShowroomDb` trong `Showroom.Web/appsettings.json`, hoac bien moi truong:

```powershell
$env:SHOWROOM_TEST_SQL_CONNECTION_STRING = "Server=...;Database=master;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;"
```

Test helper se tu tao database tam va tu dong xoa sau khi test xong.

## Loi thuong gap

### Khong gui duoc email

- Kiem tra `Smtp:Host`, `Smtp:Port`, `Smtp:Username`, `Smtp:Password`, `Smtp:FromEmail`
- Gmail phai dung App Password
- Khach hang phai co email trong yeu cau

### Thieu bang hoac cot database

Chay:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -i database\upgrade.sql
```

### SQL Server khong ket noi duoc

- Kiem tra instance SQL Server dang chay
- Kiem tra `ConnectionStrings:ShowroomDb`
- Neu dung SQL auth, doi connection string sang dang `User Id=...;Password=...;`

## Ghi chu bao mat

- Khong commit SMTP password, Cloudflare token, password hash that len GitHub
- Nen dung `user-secrets` cho moi truong local
- Password hash dung `pbkdf2-sha256`
- Login POST co rate limiting theo IP
- Tai khoan bi tam khoa sau nhieu lan dang nhap sai lien tiep
