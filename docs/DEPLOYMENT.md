# Hướng dẫn triển khai EduSpark

Tài liệu mô tả cách đưa EduSpark (ASP.NET Core 8 MVC + SQL Server) lên môi trường thật, kèm cấu hình thanh toán VNPAY. Chi tiết kỹ thuật tích hợp VNPAY xem [VNPAY_INTEGRATION.md](VNPAY_INTEGRATION.md); hướng dẫn thanh toán cho người dùng xem [PAYMENT_GUIDE.md](PAYMENT_GUIDE.md).

## 1. Yêu cầu

| Thành phần | Phiên bản / ghi chú |
|---|---|
| Runtime | ASP.NET Core Runtime **8.0** (trên IIS cài **.NET 8 Hosting Bundle**) |
| Cơ sở dữ liệu | SQL Server 2019+ / Azure SQL / SQL Server của nhà cung cấp hosting |
| HTTPS | Bắt buộc cho IPN URL của VNPAY |
| Build | .NET SDK 8 (máy build hoặc GitHub Actions) |

## 2. Ứng dụng làm gì khi khởi động

`Program.cs` tự động:

1. **Chạy migration** (`Database.MigrateAsync()`): tạo/cập nhật toàn bộ bảng, không cần chạy `dotnet ef database update` trên server.
2. **Seed dữ liệu mẫu** (`SeedData.InitializeAsync`): vai trò, danh mục, khóa học mẫu và các tài khoản:

   | Email | Mật khẩu |
   |---|---|
   | `admin@eduspark.vn` | `Admin12345` |
   | `instructor@eduspark.vn` | `Instructor12345` |
   | `student@eduspark.vn` | `Student12345` |
   | `applicant@eduspark.vn` | `Student12345` |

   > **Bắt buộc:** các tài khoản này được tạo ở **mọi** môi trường. Sau lần triển khai đầu tiên, đăng nhập và **đổi mật khẩu admin** (hoặc khóa/xóa các tài khoản mẫu) trước khi công khai website.

3. Lưu file người dùng tải lên vào `wwwroot/uploads/` (video, tài liệu, chứng chỉ PDF) và email giả lập vào `App_Data/MockEmails/`. Tài khoản chạy ứng dụng (IIS App Pool) cần **quyền ghi** hai thư mục này; cần **sao lưu** `wwwroot/uploads/` vì đây là dữ liệu người dùng.

## 3. Cấu hình

ASP.NET Core đọc cấu hình theo thứ tự (sau ghi đè trước): `appsettings.json` → `appsettings.{Environment}.json` → User Secrets (chỉ Development) → **biến môi trường**. Trên server, **không dùng User Secrets**; dùng `appsettings.Production.json` (không commit) hoặc biến môi trường (dấu `:` thay bằng `__`, ví dụ `Vnpay__HashSecret`).

### 3.1. Bắt buộc

| Khóa | Ví dụ | Ghi chú |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Biến môi trường. Bật trang lỗi thân thiện + HSTS. |
| `ConnectionStrings:DefaultConnection` | `Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True` | Mặc định trỏ LocalDB, chỉ dùng được trên máy dev. |

### 3.2. Thanh toán VNPAY

| Khóa | Sandbox | Production |
|---|---|---|
| `Vnpay:TmnCode` | Mã VNPAY gửi qua email đăng ký sandbox | Mã production VNPAY cấp |
| `Vnpay:HashSecret` | Secret sandbox | Secret production (**tuyệt đối không commit**) |
| `Vnpay:BaseUrl` | `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html` | URL production VNPAY cấp |
| `Vnpay:UseRealGateway` | `true` | `true` |
| `Vnpay:UpdateOrderOnReturn` | `true` nếu chưa khai báo IPN | **`false`** (chỉ IPN cập nhật đơn) |
| `Vnpay:ExpireMinutes` | `15` | `15` |

Nếu bật cổng thật mà thiếu `TmnCode`/`HashSecret`, học viên nhận thông báo "Cổng VNPAY chưa được cấu hình" thay vì bị chuyển sang VNPAY với chữ ký sai.

### 3.3. Chế độ giả lập (`AppFeatures:UseMockServices`)

`appsettings.json` mặc định `true`: Email, YouTube, MoMo và VNPAY đều chạy giả lập.

- Chỉ muốn VNPAY thật: giữ `UseMockServices = true`, đặt `Vnpay:UseRealGateway = true`.
- Muốn tắt hết giả lập: `UseMockServices = false` **và** cấu hình đủ `Smtp:*`, `YouTube:*`, `Momo:*` (xem README mục 4).

Trang giả lập `/Payment/MockGateway` tự động trả 404 khi cổng tương ứng chạy thật, nên không thể dùng để "thanh toán giả" trên production.

### 3.4. Tùy chọn khác

`Authentication:Google:ClientId/ClientSecret` (đăng nhập Google, chỉ bật khi có đủ hai khóa), `Smtp:*`, `YouTube:*`, `Momo:*`.

## 4. Cách 1 - Tự động bằng GitHub Actions lên Somee (đang dùng)

Workflow `.github/workflows/deploy-somee.yml` chạy mỗi khi push lên `main` (hoặc bấm *Run workflow*):

1. `dotnet publish -c Release`.
2. Sinh `publish/appsettings.Production.json` từ GitHub Secrets/Variables.
3. Upload thư mục `publish/` qua FTP lên `/www.ltwnhom10.somee.com/`.

### 4.1. Khai báo Secrets / Variables

Vào **GitHub repo → Settings → Secrets and variables → Actions**:

| Loại | Tên | Bắt buộc | Giá trị |
|---|---|---|---|
| Secret | `PROD_CONNECTION_STRING` | Có | Chuỗi kết nối SQL Server trên Somee |
| Secret | `SOMEE_FTP_SERVER` | Có | FTP host Somee |
| Secret | `SOMEE_FTP_USERNAME` | Có | FTP user |
| Secret | `SOMEE_FTP_PASSWORD` | Có | FTP password |
| Secret | `VNPAY_TMN_CODE` | Để bật VNPAY thật | `vnp_TmnCode` |
| Secret | `VNPAY_HASH_SECRET` | Để bật VNPAY thật | `vnp_HashSecret` |
| Variable | `VNPAY_BASE_URL` | Không | Bỏ trống = URL sandbox |
| Variable | `VNPAY_UPDATE_ORDER_ON_RETURN` | Không | `false` sau khi đã khai báo IPN URL với VNPAY |

Chỉ khi có **cả** `VNPAY_TMN_CODE` và `VNPAY_HASH_SECRET`, workflow mới ghi section `Vnpay` (kèm `UseRealGateway = true`); thiếu một trong hai thì website tiếp tục chạy VNPAY giả lập.

Bằng GitHub CLI (chạy trong thư mục repo):

```bash
gh secret set VNPAY_TMN_CODE
gh secret set VNPAY_HASH_SECRET
gh variable set VNPAY_UPDATE_ORDER_ON_RETURN --body "true"
```

### 4.2. Triển khai

```bash
git checkout main
git merge <branch-tính-năng>
git push origin main
```

Theo dõi tại tab **Actions** của repo (hoặc `gh run watch`). Lần chạy đầu tiên, ứng dụng tự tạo bảng trong database Somee khi có request đầu tiên.

## 5. Cách 2 - Triển khai thủ công lên Windows Server / IIS

1. **Cài trên server:** .NET 8 Hosting Bundle (khởi động lại IIS sau khi cài: `iisreset`), SQL Server (hoặc dùng SQL từ xa).
2. **Publish trên máy build:**
   ```bash
   cd QuanLiKhoaHocTrucTuyen
   dotnet publish -c Release -o ./publish
   ```
3. **Copy** thư mục `publish/` lên server, ví dụ `C:\inetpub\eduspark`.
4. **Tạo site IIS:** Application Pool với *.NET CLR version = No Managed Code*, trỏ Physical path vào thư mục trên, gắn binding HTTPS kèm chứng chỉ SSL.
5. **Cấu hình:** tạo `C:\inetpub\eduspark\appsettings.Production.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=.;Database=QuanLiKhoaHocTrucTuyenDb;User Id=eduspark;Password=<MAT_KHAU>;TrustServerCertificate=True"
     },
     "Vnpay": {
       "TmnCode": "<TMN_CODE>",
       "HashSecret": "<HASH_SECRET>",
       "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
       "UseRealGateway": true,
       "UpdateOrderOnReturn": false
     }
   }
   ```
   hoặc đặt biến môi trường trong `web.config` / App Pool (`ConnectionStrings__DefaultConnection`, `Vnpay__HashSecret`, ...).
6. **Phân quyền ghi** cho `IIS AppPool\<TênPool>` trên `wwwroot\uploads` và `App_Data` (tạo thư mục nếu chưa có).
7. Truy cập website để ứng dụng tự chạy migration + seed, rồi **đổi mật khẩu admin**.

> Chạy sau reverse proxy (Nginx, Cloudflare, load balancer) cần bật `ForwardedHeaders` để `vnp_IpAddr` và `vnp_ReturnUrl` dùng đúng IP/scheme/domain của khách; nếu không, `vnp_ReturnUrl` có thể bị sinh với `http://` hoặc host nội bộ.

## 6. Cấu hình phía VNPAY

1. **Sandbox:** đăng ký tại <https://pay.vnpaytest.vn/dev-registration> để nhận `TmnCode` + `HashSecret`.
2. **IPN URL:** khai báo `https://<domain>/Payment/VnpayIpn` tại trang quản trị merchant (sandbox: <https://sandbox.vnpayment.vn/merchantv2/>) hoặc gửi cho VNPAY. IPN là kênh server-to-server, **bắt buộc HTTPS** và phải truy cập được từ Internet.
3. Sau khi IPN hoạt động, đặt `Vnpay:UpdateOrderOnReturn = false` (hoặc Variable `VNPAY_UPDATE_ORDER_ON_RETURN = false`).
4. **Lên production:** ký hợp đồng với VNPAY, nhận `TmnCode`/`HashSecret`/`BaseUrl` production, cập nhật cấu hình; giữ nguyên code.

## 7. Kiểm tra sau triển khai

| Kiểm tra | Cách làm | Kết quả mong đợi |
|---|---|---|
| Website chạy | Mở trang chủ | Hiển thị danh sách khóa học |
| IPN endpoint | `curl https://<domain>/Payment/VnpayIpn` | `{"RspCode":"99","Message":"Input data required"}` |
| Chặn chữ ký giả | `curl "https://<domain>/Payment/VnpayIpn?vnp_TxnRef=X&vnp_SecureHash=abc"` | `{"RspCode":"97","Message":"Invalid signature"}` |
| Trang mock bị tắt | Đăng nhập, mở `/Payment/MockGateway?orderCode=X&provider=VNPAY` | 404 (khi VNPAY chạy thật) |
| Thanh toán sandbox | Làm theo [PAYMENT_GUIDE.md](PAYMENT_GUIDE.md) mục 2 với thẻ NCB test | Đơn "Đã thanh toán", khóa học xuất hiện trong "Khóa học của tôi" |
| Bảo mật | Đăng nhập `admin@eduspark.vn` bằng mật khẩu mẫu | **Phải thất bại** (đã đổi mật khẩu) |

## 8. Xử lý sự cố

| Hiện tượng | Nguyên nhân thường gặp | Cách xử lý |
|---|---|---|
| HTTP 500.30 / 500.31 trên IIS | Thiếu .NET 8 Hosting Bundle hoặc lỗi khi khởi động (thường do chuỗi kết nối) | Cài Hosting Bundle; bật `stdoutLogEnabled="true"` trong `web.config` để xem log |
| Lỗi kết nối SQL khi khởi động | Chuỗi kết nối sai / SQL chặn truy cập | Kiểm tra `DefaultConnection`, firewall, user SQL có quyền tạo bảng (migration) |
| Trang VNPAY báo "Sai chữ ký" | `HashSecret` sai/thừa khoảng trắng, hoặc `TmnCode` không khớp secret | Copy lại đúng giá trị từ email VNPAY |
| Trang VNPAY báo website/merchant không hợp lệ | `TmnCode` sai hoặc chưa kích hoạt | Kiểm tra email đăng ký sandbox |
| "Cổng VNPAY chưa được cấu hình" | Bật `UseRealGateway` nhưng thiếu `TmnCode`/`HashSecret` | Bổ sung cấu hình rồi khởi động lại site |
| Thanh toán xong nhưng trang báo "Đang xác nhận thanh toán" | `UpdateOrderOnReturn = false` mà IPN chưa tới | Kiểm tra IPN URL đã khai báo, HTTPS hợp lệ; xem bảng `Transactions` |
| Không upload được video/tài liệu | Thiếu quyền ghi `wwwroot/uploads` hoặc file > 150 MB | Cấp quyền ghi; kiểm tra giới hạn upload của hosting |

Mọi callback VNPAY (kể cả sai chữ ký, lệch số tiền) đều được lưu trong bảng `Transactions` (cột `RawPayload`, `SignatureValid`) để đối soát.
