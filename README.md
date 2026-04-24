# Auto Car Showroom Chatbox AI

Ung dung ASP.NET Core MVC de quan ly showroom o to, bao gom:

- Dang nhap khu vuc quan tri voi phan quyen `Administrator` va `Staff`
- Quan ly hang xe, xe, don hang
- Nhat ky thao tac quan tri
- Dashboard thong ke ton kho va xe ban chay co bo loc thoi gian

## Yeu cau

- .NET SDK 8.0
- SQL Server
- PowerShell tren Windows

## Khoi tao database

Chay `database/setup.sql` de tao lai database, schema va du lieu mau:

```sql
:r database/setup.sql
```

Neu database da ton tai va ban chi muon bo sung rang buoc / bang nhat ky, chay:

```sql
:r database/upgrade.sql
```

## Cau hinh tai khoan admin an toan

Repo khong con luu san ten dang nhap va password hash. Hay dung `user-secrets` hoac bien moi truong.

Tao password hash:

```powershell
.\generate-admin-password-hash.ps1 -Password "Admin@123"
```

Gan bang `user-secrets`:

```powershell
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:Username" "admin"
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:PasswordHash" "<PASTE_HASH_HERE>"
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:DisplayName" "Quan tri vien"
dotnet user-secrets set --project .\Showroom.Web "AdminCredentials:Accounts:0:Role" "Administrator"
```

Tai khoan nhan vien co the them tuong tu voi index `1` va role `Staff`.

Hoac dung bien moi truong:

```powershell
$env:AdminCredentials__Accounts__0__Username = "admin"
$env:AdminCredentials__Accounts__0__PasswordHash = "<PASTE_HASH_HERE>"
$env:AdminCredentials__Accounts__0__DisplayName = "Quan tri vien"
$env:AdminCredentials__Accounts__0__Role = "Administrator"
```

## Chay ung dung

```powershell
dotnet run --project .\Showroom.Web
```

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

## Ghi chu bao mat

- Password hash moi dung `pbkdf2-sha256`
- Hash cu `pbkdf2-sha1` van duoc verify de tuong thich nguoc
- Login POST co rate limiting theo IP
- Tai khoan bi tam khoa sau nhieu lan dang nhap sai lien tiep
