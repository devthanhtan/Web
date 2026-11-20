# ASSUMPTIONS - Giả định và Phương án Kỹ thuật

Tài liệu này ghi nhận các quyết định thiết kế cho những điểm đặc tả mở để đảm bảo hệ thống chạy trọn luồng trên Visual Studio / .NET 8 và SQL Server LocalDB.

1. **Định dạng Solution**: Sử dụng file `QuanLiKhoaHocTrucTuyen.slnx` đã được khởi tạo trong thư mục dự án (tương thích Visual Studio 2022 17.13+ / VS 2026 và `dotnet` CLI). Project kiểm thử `QuanLiKhoaHocTrucTuyen.Tests` (xUnit, .NET 8) được thêm vào cùng solution này.
2. **CSDL mặc định**: SQL Server LocalDB `(localdb)\mssqllocaldb`, tên database `QuanLiKhoaHocTrucTuyenDb`.
3. **Authentication & Identity UI**:
   - Sử dụng `AddIdentity<ApplicationUser, IdentityRole>` kết hợp `AddDefaultUI()` và các Razor Pages tiếng Việt viết tường minh trong `Areas/Identity/Pages/Account/` (`Login`, `Register`, `RegisterConfirmation`, `ConfirmEmail`, `ExternalLogin`, `ForgotPassword`, `ResetPassword`, `AccessDenied`, `Logout`).
   - Cấu hình Google External Login có điều kiện: chỉ đăng ký `.AddGoogle(...)` khi `Authentication:Google:ClientId` và `ClientSecret` được cấu hình hợp lệ trong User Secrets / `appsettings`, giúp ứng dụng khởi động bình thường kể cả khi chưa điền khóa Google.
4. **Chế độ giả lập (`UseMockServices: true`)**:
   - **Email (`MailKitEmailSender`)**: Khi `UseMockServices = true` hoặc chưa cấu hình SMTP, email vẫn được ghi log qua `ILogger` và lưu bản xem nhanh (`.html`) trong `App_Data/MockEmails` + hiển thị liên kết xác thực trực tiếp trên trang `RegisterConfirmation` để kiểm thử nhanh luồng `RequireConfirmedAccount`.
   - **YouTube Data API (`VideoUploadWorker` + `YouTubeUploadService`)**: Vẫn chạy bất đồng bộ qua `BackgroundService` và `Channel<int>`. Ở chế độ mock, video tải lên được giữ file tạm an toàn tại `wwwroot/uploads/videos/` và gán mã mock `MOCK_<lessonId>` (hoặc YouTube ID mẫu cho dữ liệu seed). Trình phát ở trang học hỗ trợ cả **YouTube IFrame Player API** (với YouTube Video ID thật) lẫn **HTML5 Video Player Adapter** (khi ở chế độ mock local), cùng dùng chung bộ theo dõi JavaScript delta gửi Ajax về server để cộng dồn `WatchedSeconds` (chống tua nhanh) và lưu `LastPositionSeconds`.
   - **VNPAY & MoMo (`PaymentGatewayService`)**: Sử dụng đúng hàm băm `HMACSHA512` (VNPAY) và `HMACSHA256` (MoMo). Ở chế độ mock, ứng dụng chuyển tới trang `/Payment/MockGateway` mô phỏng cổng thanh toán sandbox, tự ký chữ ký HMAC chuẩn rồi gọi về đúng `ReturnUrl` và `IPN` endpoint để kiểm tra xác minh chữ ký, đối chiếu số tiền và xử lý idempotent.
5. **Thời lượng video (`DurationSeconds`)**: Khi Giảng viên tải video lên, JavaScript trên form tự động đọc metadata `<video>` để điền `DurationSeconds` (có thể chỉnh tay hoặc mặc định 600s nếu không đọc được), giúp tính ngưỡng hoàn thành `>= 90%` (BR-D3) chính xác mà không cần cài `ffmpeg`.
6. **Học thử & Xem trước (BR-C1)**: Bổ sung `Course.IntroYoutubeId` và `Lesson.IsFreePreview` để học viên chưa mua vẫn xem được video giới thiệu / bài học thử trên trang chi tiết khóa học.
7. **Ngân hàng câu hỏi & Thi lại (BR-E1..E6)**:
   - `Question` thuộc về `Course` (ngân hàng câu hỏi của khóa), liên kết vào `Quiz` qua bảng trung gian `QuizQuestion` để Giảng viên tái sử dụng câu hỏi cho nhiều bài kiểm tra chương hoặc bài thi cuối khóa (`IsFinalExam = true`).
   - `Quiz.MaxAttempts = 0` nghĩa là không giới hạn số lần thi lại; `MaxAttempts > 0` giới hạn đúng số lần thi lại của học viên.
   - `QuizAttempt` lưu `StartedAt` và `ExpiresAt` trên server; khi nộp bài, server đối chiếu `DateTime.UtcNow <= ExpiresAt + 10s` (grace period) và tự động chấm điểm từ câu trả lời hiện có nếu hết giờ.
8. **Chứng chỉ PDF + QR (BR-E5)**:
   - Sử dụng `QuestPDF` (`LicenseType.Community`) và `QRCoder` (`PngByteQRCode`).
   - Mã chứng chỉ duy nhất dạng `CERT-YYYY-XXXXXXXX`, lưu dữ liệu QR là URL tra cứu công khai `/Certificate/Verify?code=...` và lưu file PDF vào `wwwroot/uploads/certificates/`.
9. **Hoa hồng tại thời điểm thanh toán (BR-G1)**: `OrderItem` chụp lại `CommissionRatePercent`, `PlatformAmount` và `InstructorAmount` tại thời điểm `Order` chuyển sang `Paid`, đảm bảo thay đổi cấu hình hoa hồng sau này không làm sai lệch báo cáo lịch sử.
10. **Hộp thư hỏi đáp học viên (UI Instructor)**: Bổ sung thực thể `CourseQuestion` để học viên đang học có thể đặt câu hỏi theo khóa/bài học và Giảng viên trả lời trong `Areas/Instructor/Qna`.
