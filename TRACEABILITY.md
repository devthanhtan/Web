# TRACEABILITY MATRIX - Đối chiếu Quy tắc Nghiệp vụ (BR-xx)

Tài liệu đối chiếu từng quy tắc nghiệp vụ bắt buộc với file, class, action và kiểm thử tương ứng trong dự án `QuanLiKhoaHocTrucTuyen`.

## A. Tài khoản và phân quyền
| Mã BR | Yêu cầu nghiệp vụ | File / Class / Action tương ứng | Trạng thái |
|---|---|---|---|
| **BR-A1** | Đăng ký email + mật khẩu; Google external login | `Areas/Identity/Pages/Account/Register.cshtml(.cs)`, `ExternalLogin.cshtml(.cs)`, `Program.cs` (`AddGoogle`) | Đáp ứng (Google bật khi có `ClientId`/`ClientSecret`) |
| **BR-A2** | Email chưa tồn tại; mật khẩu tối thiểu 8 ký tự, có chữ và số | `Program.cs` (`PasswordOptions`, `RequireUniqueEmail = true`), `Register.cshtml.cs` | Đáp ứng |
| **BR-A3** | `RequireConfirmedAccount = true`, gửi mail xác thực (`IEmailSender`), gán mặc định role `Student` | `Program.cs`, `Services/MailKitEmailSender.cs`, `Register.cshtml.cs`, `ConfirmEmail.cshtml.cs`, `RegisterConfirmation.cshtml.cs` | Đáp ứng |
| **BR-A4** | Cập nhật hồ sơ (ảnh đại diện, SĐT, giới thiệu) trên `ApplicationUser` | `Models/ApplicationUser.cs`, `Areas/Student/Controllers/ProfileController.cs`, `Areas/Student/Views/Profile/Index.cshtml` | Đáp ứng |
| **BR-A5** | Học viên gửi yêu cầu nâng cấp lên Giảng viên kèm CV, chứng chỉ (upload file) | `Models/InstructorRequest.cs`, `Services/InstructorRequestService.cs` (`SubmitOrResubmitAsync`), `Areas/Student/Controllers/InstructorApplicationController.cs` | Đáp ứng |
| **BR-A6** | Admin phê duyệt (cấp role `Instructor`, mở dashboard) hoặc từ chối (bắt buộc lý do, hiển thị/gửi lại) | `Services/InstructorRequestService.cs` (`ApproveAsync`, `RejectAsync`), `Areas/Admin/Controllers/InstructorRequestsController.cs`, `Areas/Admin/Views/InstructorRequests/Index.cshtml` | Đáp ứng |
| **BR-A7** | Admin xem, tìm kiếm, khóa/mở khóa người dùng | `Services/UserManagementService.cs`, `Areas/Admin/Controllers/UsersController.cs`, `Areas/Admin/Views/Users/Index.cshtml` | Đáp ứng |

## B. Quản lý khóa học
| Mã BR | Yêu cầu nghiệp vụ | File / Class / Action tương ứng | Trạng thái |
|---|---|---|---|
| **BR-B1** | Giảng viên tạo/sửa khóa học (tên, danh mục, mô tả, ảnh, học phí) | `Services/CourseManagementService.cs` (`CreateCourseAsync`, `UpdateCourseAsync`), `Areas/Instructor/Controllers/CoursesController.cs` (`Create`, `Edit`) | Đáp ứng |
| **BR-B2** | Cấu trúc Khóa học -> Chương -> Bài học (thêm, sửa, xóa, sắp xếp), loại Video/Tài liệu | `Models/Course.cs`, `Chapter.cs`, `Lesson.cs`, `CourseManagementService.cs`, `Areas/Instructor/Views/Courses/Curriculum.cshtml` | Đáp ứng |
| **BR-B3** | Upload video bài giảng & đính kèm tài liệu (giới hạn dung lượng) | `Services/LocalFileStorageService.cs`, `CourseManagementService.cs` (`AddLessonAsync`, `UpdateLessonAsync`), `Options/AppOptions.cs` | Đáp ứng |
| **BR-B4** | Đăng video lên YouTube chế độ `unlisted` qua `BackgroundService`, lưu `YoutubeVideoId`, trạng thái `Processing/Ready/Error`, báo lỗi & cho tải lại | `Services/YouTubeUploadService.cs`, `Services/VideoUploadWorker.cs`, `Areas/Instructor/Views/Courses/Curriculum.cshtml`, `EditLesson.cshtml` | Đáp ứng (kèm Mock mode) |
| **BR-B5** | Trạng thái `Draft -> PendingReview -> Approved/Rejected`; `Draft` chỉ Giảng viên sở hữu thấy | `Models/Enums.cs` (`CourseStatus`), `CourseManagementService.cs` (`GetCourseOwnedByInstructorAsync`, `GetCoursesForAdminReviewAsync`) | Đáp ứng |
| **BR-B6** | Chỉ gửi duyệt khi có nội dung và mọi video đều `Ready`; chặn và báo rõ bài chưa sẵn sàng | `CourseManagementService.cs` (`SubmitForReviewAsync`), `Areas/Instructor/Controllers/CoursesController.cs` (`SubmitForReview`), `CourseReviewAndCartRulesTests.cs` | Đáp ứng |
| **BR-B7** | Admin kiểm duyệt: Đạt -> `Approved` công khai; Không đạt -> `Rejected` kèm lý do bắt buộc | `Areas/Admin/Controllers/CoursesController.cs` (`Approve`, `Reject`), `Areas/Admin/Views/Courses/Details.cshtml` | Đáp ứng |

## C. Mua khóa học và thanh toán
| Mã BR | Yêu cầu nghiệp vụ | File / Class / Action tương ứng | Trạng thái |
|---|---|---|---|
| **BR-C1** | Tìm kiếm, lọc danh mục/giá/đánh giá, xem video giới thiệu hoặc học thử, thêm vào giỏ | `Services/CourseCatalogService.cs`, `Controllers/CoursesController.cs` (`Index`, `Details`), `Views/Courses/Index.cshtml`, `Details.cshtml` | Đáp ứng |
| **BR-C2** | Không cho thêm vào giỏ khóa học đã sở hữu (chặn ở View lẫn Controller/Service) | `Views/Courses/Details.cshtml`, `Services/CartService.cs` (`AddToCartAsync`), `CourseReviewAndCartRulesTests.cs` | Đáp ứng |
| **BR-C3** | Tạo `Order` với mã giao dịch duy nhất, `PendingPayment`, chuyển sang VNPAY/MoMo (sandbox) | `Services/PaymentGatewayService.cs` (`CreateOrderAndPaymentUrlAsync`), `Areas/Student/Controllers/CartController.cs` (`Checkout`) | Đáp ứng |
| **BR-C4** | Xử lý ReturnUrl & IPN: xác minh HMAC, đối chiếu mã GD & số tiền, xử lý idempotent | `Controllers/PaymentController.cs` (`VnpayReturn`, `VnpayIpn`, `MomoReturn`, `MomoIpn`), `PaymentGatewayService.cs` (`VerifyVnpaySignature`, `VerifyMomoSignature`), `PaymentSignatureAndIdempotencyTests.cs` | Đáp ứng |
| **BR-C5** | Thành công: `Paid`, lưu `Transaction`, mở quyền truy cập (`Enrollment`), xóa giỏ hàng | `PaymentGatewayService.cs` (`CompleteOrderPaidAsync`), `PaymentSignatureAndIdempotencyTests.cs` | Đáp ứng |
| **BR-C6** | Thất bại hoặc hủy: `Cancelled`, giữ khóa học trong giỏ, cho phép thanh toán lại | `PaymentGatewayService.cs` (`MarkOrderCancelledAsync`, `RetryOrderPaymentAsync`), `Areas/Student/Controllers/OrdersController.cs` (`Retry`) | Đáp ứng |

## D. Học tập và tiến độ
| Mã BR | Yêu cầu nghiệp vụ | File / Class / Action tương ứng | Trạng thái |
|---|---|---|---|
| **BR-D1** | Chỉ học viên sở hữu mới vào giao diện học, xem YouTube IFrame Player API, tải tài liệu | `Areas/Student/Controllers/LearnController.cs` (`Index`, `DownloadAttachment`), `Areas/Student/Views/Learn/Index.cshtml` | Đáp ứng |
| **BR-D2** | JS bắt sự kiện phát/tạm dừng/kết thúc gửi Ajax POST định kỳ; server cộng dồn thời lượng thực (không tính tua), lưu `LastPositionSeconds` | `Areas/Student/Views/Learn/Index.cshtml` (JS tracker), `LearnController.cs` (`TrackProgress`), `LearningProgressService.cs` (`RecordWatchProgressAsync`) | Đáp ứng |
| **BR-D3** | Video hoàn thành khi `WatchedSeconds >= 90%` thời lượng; tài liệu hoàn thành khi mở xem | `LearningProgressService.cs` (`IsVideoCompletionReached`, `MarkDocumentLessonCompletedAsync`), `LearningProgressThresholdTests.cs` | Đáp ứng |
| **BR-D4** | Tiến độ % = bài hoàn thành / tổng bài, cập nhật Ajax realtime bằng thanh tiến độ Bootstrap | `LearningProgressService.cs` (`RecalculateCourseProgressAsync`), `Areas/Student/Views/Learn/Index.cshtml` (`#courseProgressBar`) | Đáp ứng |

## E. Kiểm tra và chứng chỉ
| Mã BR | Yêu cầu nghiệp vụ | File / Class / Action tương ứng | Trạng thái |
|---|---|---|---|
| **BR-E1** | Giảng viên xây ngân hàng câu hỏi, tạo quiz chương/cuối khóa, thiết lập thời gian & điểm chuẩn | `Areas/Instructor/Controllers/QuestionsController.cs`, `QuizzesController.cs`, `Models/Question.cs`, `Quiz.cs` | Đáp ứng |
| **BR-E2** | Nhập hàng loạt câu hỏi từ `.xlsx`/`.csv`, tải file mẫu, báo lỗi từng dòng khi sai định dạng | `Services/QuestionImportService.cs` (`ClosedXML`), `Areas/Instructor/Controllers/QuestionsController.cs` (`Import`, `DownloadTemplate`) | Đáp ứng |
| **BR-E3** | Bài thi cuối khóa chỉ mở khi hoàn thành 100% bài học | `Services/QuizService.cs` (`IsFinalExamUnlocked`, `CheckEligibilityAsync`), `Learn/Index.cshtml`, `Quiz/Info.cshtml`, `QuizAndExamEligibilityTests.cs` | Đáp ứng |
| **BR-E4** | Đồng hồ đếm ngược, hết giờ tự nộp (kiểm tra cả client & server), chấm tự động & hiển thị ngay | `Areas/Student/Views/Quiz/Take.cshtml`, `Areas/Student/Controllers/QuizController.cs` (`Submit`, `Result`), `QuizService.cs` (`SubmitAttemptAsync`) | Đáp ứng |
| **BR-E5** | Đạt -> cấp chứng chỉ PDF + mã định danh + mã QR (`QuestPDF` + `QRCoder`); trang công khai tra cứu & xác thực | `Services/CertificateService.cs`, `Areas/Student/Controllers/CertificatesController.cs`, `Controllers/CertificateController.cs` (`Verify`, `QrImage`, `DownloadPdf`) | Đáp ứng |
| **BR-E6** | Không đạt -> xem lại kết quả và thi lại theo `MaxAttempts` của Giảng viên | `Areas/Student/Views/Quiz/Result.cshtml`, `QuizService.cs` (`CheckEligibilityAsync`), `QuizAndExamEligibilityTests.cs` | Đáp ứng |

## F. Đánh giá
| Mã BR | Yêu cầu nghiệp vụ | File / Class / Action tương ứng | Trạng thái |
|---|---|---|---|
| **BR-F1** | Chỉ học viên sở hữu có tiến độ >= 50% mới được đánh giá 1-5 sao + nhận xét | `Services/ReviewService.cs` (`CanStudentReview`, `SaveReviewAsync`), `Areas/Student/Controllers/ReviewsController.cs`, `ReviewEligibilityTests.cs` | Đáp ứng |
| **BR-F2** | Chưa đạt 50% thì vô hiệu hóa nút đánh giá & hiển thị thông báo tiếp tục học | `Areas/Student/Views/Learn/Index.cshtml` (`#tabReview`) | Đáp ứng |
| **BR-F3** | Mỗi học viên đánh giá 1 lần/khóa (unique index), được sửa đánh giá của mình | `Data/ApplicationDbContext.cs` (`HasIndex(UserId, CourseId).IsUnique()`), `ReviewService.cs` (`SaveReviewAsync`), `ReviewEligibilityTests.cs` | Đáp ứng |
| **BR-F4** | Trang khóa học hiển thị công khai điểm trung bình & danh sách nhận xét | `Services/CourseCatalogService.cs`, `Controllers/CoursesController.cs` (`Details`), `Views/Courses/Details.cshtml` | Đáp ứng |
| **BR-F5** | Admin kiểm soát đánh giá (ẩn hoặc xóa đánh giá vi phạm) | `Areas/Admin/Controllers/ReviewsController.cs` (`ToggleHide`, `Delete`), `Areas/Admin/Views/Reviews/Index.cshtml` | Đáp ứng |

## G. Hoa hồng, thống kê, báo cáo
| Mã BR | Yêu cầu nghiệp vụ | File / Class / Action tương ứng | Trạng thái |
|---|---|---|---|
| **BR-G1** | Admin cấu hình tỷ lệ chiết khấu/hoa hồng; mỗi đơn `Paid` ghi nhận phần nền tảng & giảng viên theo tỷ lệ tại thời điểm thanh toán | `Areas/Admin/Controllers/CommissionController.cs`, `Models/CommissionConfig.cs`, `OrderItem.cs`, `PaymentGatewayService.cs` (`CompleteOrderPaidAsync`) | Đáp ứng |
| **BR-G2** | Tổng hợp số liệu theo tuần hoặc tháng (chọn được kỳ) | `Services/ReportService.cs` (`BuildBuckets` với `period = week | month`) | Đáp ứng |
| **BR-G3** | Giảng viên xem thống kê khóa học cá nhân: doanh thu, học viên mới, đánh giá | `Areas/Instructor/Controllers/DashboardController.cs`, `Areas/Instructor/Views/Dashboard/Index.cshtml` (Chart.js) | Đáp ứng |
| **BR-G4** | Admin xem báo cáo toàn hệ thống: doanh thu, phân chia hoa hồng, biến động tài khoản, khóa học tương tác cao, Chart.js & xuất CSV | `Areas/Admin/Controllers/DashboardController.cs` (`Index`, `ExportCsv`), `Areas/Admin/Views/Dashboard/Index.cshtml` (Chart.js + Xuất CSV) | Đáp ứng |
