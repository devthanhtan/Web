# EduSpark - Hệ thống Bán và Học Khóa Học Trực Tuyến (ASP.NET Core 8 MVC)

Website bán và học khóa học trực tuyến xây dựng trên **ASP.NET Core 8.0 MVC**, **Entity Framework Core 8**, **SQL Server LocalDB** và **ASP.NET Core Identity (Individual Accounts)**.

## 1. Yêu cầu môi trường
- Visual Studio 2022 (17.13+ hoặc VS 2026) hoặc .NET 8 SDK
- SQL Server Express LocalDB (`(localdb)\mssqllocaldb`)

## 2. Cấu hình & Khởi chạy nhanh
1. Mở file `QuanLiKhoaHocTrucTuyen.slnx` bằng Visual Studio.
2. Kiểm tra chuỗi kết nối trong `appsettings.json` (mặc định dùng LocalDB `QuanLiKhoaHocTrucTuyenDb`).
3. Áp dụng Migration vào CSDL (trong Package Manager Console của Visual Studio):
   ```powershell
   Update-Database
   ```
   Hoặc qua CLI:
   ```powershell
   dotnet tool restore
   dotnet tool run dotnet-ef database update --project QuanLiKhoaHocTrucTuyen
   ```
4. Nhấn **F5** trong Visual Studio để chạy ứng dụng (hệ thống tự seed vai trò, tài khoản và khóa học mẫu).

## 3. Tài khoản mẫu được Seed sẵn
| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | `admin@eduspark.vn` | `Admin12345` |
| Giảng viên | `instructor@eduspark.vn` | `Instructor12345` |
| Học viên | `student@eduspark.vn` | `Student12345` |
| Học viên đang chờ duyệt GV | `applicant@eduspark.vn` | `Student12345` |

## 4. Chế độ Giả lập (`UseMockServices`) & Cấu hình API thật (User Secrets)
Mặc định `AppFeatures:UseMockServices = true` trong `appsettings.Development.json` để chạy thử toàn bộ luồng (Email, Upload YouTube, VNPAY, MoMo) mà không cần khóa thật.

Khi muốn kết nối dịch vụ thật, đặt `AppFeatures:UseMockServices = false` và cấu hình bằng **User Secrets** (chuột phải project -> *Manage User Secrets*):
```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "<YOUR_GOOGLE_CLIENT_ID>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<YOUR_GOOGLE_CLIENT_SECRET>"
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Smtp:Port" "587"
dotnet user-secrets set "Smtp:UserName" "<YOUR_EMAIL>"
dotnet user-secrets set "Smtp:Password" "<YOUR_APP_PASSWORD>"
dotnet user-secrets set "YouTube:ClientId" "<YOUR_YT_CLIENT_ID>"
dotnet user-secrets set "YouTube:ClientSecret" "<YOUR_YT_CLIENT_SECRET>"
dotnet user-secrets set "YouTube:RefreshToken" "<YOUR_YT_REFRESH_TOKEN>"
dotnet user-secrets set "Vnpay:TmnCode" "<YOUR_TMN_CODE>"
dotnet user-secrets set "Vnpay:HashSecret" "<YOUR_VNPAY_SECRET>"
dotnet user-secrets set "Momo:PartnerCode" "<YOUR_PARTNER_CODE>"
dotnet user-secrets set "Momo:AccessKey" "<YOUR_ACCESS_KEY>"
dotnet user-secrets set "Momo:SecretKey" "<YOUR_MOMO_SECRET>"
```

## 5. Chạy Unit Tests (xUnit)
```powershell
dotnet test QuanLiKhoaHocTrucTuyen.slnx
```
