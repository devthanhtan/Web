using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure roles exist
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed Admin
        var admin = await EnsureUserAsync(
            userManager,
            email: "admin@eduspark.vn",
            password: "Admin12345",
            fullName: "Quản Trị Viên HUIT Learning",
            headline: "Quản trị hệ thống đào tạo trực tuyến HUIT Learning",
            bio: "Điều hành kiểm duyệt nội dung, chất lượng giảng viên và tài chính nền tảng HUIT Learning.",
            roles: [AppRoles.Admin]);

        // Seed Instructor
        var instructor = await EnsureUserAsync(
            userManager,
            email: "instructor@eduspark.vn",
            password: "Instructor12345",
            fullName: "ThS. Nguyễn Minh Khôi",
            headline: "Senior .NET & Cloud Architect · 10 năm kinh nghiệm",
            bio: "Giảng viên chuyên sâu về ASP.NET Core, Entity Framework Core, Kiến trúc hệ thống và React/UI hiện đại.",
            roles: [AppRoles.Instructor, AppRoles.Student]);

        // Seed primary Student
        var student = await EnsureUserAsync(
            userManager,
            email: "student@eduspark.vn",
            password: "Student12345",
            fullName: "Trần Bảo Ngọc",
            headline: "Sinh viên Công nghệ thông tin · Đam mê Fullstack Web",
            bio: "Đang theo học lộ trình ASP.NET Core MVC và thiết kế giao diện web.",
            roles: [AppRoles.Student]);

        // Seed secondary Student with a pending InstructorRequest so Admin sees sample review item immediately
        var applicant = await EnsureUserAsync(
            userManager,
            email: "applicant@eduspark.vn",
            password: "Student12345",
            fullName: "Lê Hoàng Nam",
            headline: "UI/UX Lead & Frontend Mentor",
            bio: "Hơn 5 năm đào tạo thiết kế sản phẩm số và Frontend.",
            roles: [AppRoles.Student]);

        if (!await context.InstructorRequests.AnyAsync())
        {
            context.InstructorRequests.Add(new InstructorRequest
            {
                UserId = applicant.Id,
                Expertise = "Thiết kế UI/UX & Frontend Bootstrap/Tailwind",
                ExperienceYears = 5,
                Bio = "Đã trực tiếp thiết kế hơn 30 hệ thống SaaS & EdTech và hướng dẫn các lớp thực chiến.",
                CvFilePath = "/samples/cv-mau.txt",
                CertificateFilePath = "/samples/chung-chi-mau.txt",
                Status = InstructorRequestStatus.Pending,
                SubmittedAt = DateTime.UtcNow.AddDays(-1)
            });
            await context.SaveChangesAsync();
        }

        // Seed default commission config (20% platform fee, 80% instructor)
        if (!await context.CommissionConfigs.AnyAsync())
        {
            context.CommissionConfigs.Add(new CommissionConfig
            {
                PlatformRatePercent = 20.00m,
                Note = "Tỷ lệ mặc định nền tảng 20% / Giảng viên 80%",
                UpdatedByAdminId = admin.Id,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // Seed Categories
        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { Name = "Lập trình Web", Slug = "lap-trinh-web", Description = "ASP.NET Core, HTML/CSS, JavaScript và kiến trúc Web hiện đại", IconClass = "bi-code-slash" },
                new Category { Name = "Cơ sở dữ liệu", Slug = "co-so-du-lieu", Description = "SQL Server, Entity Framework Core và tối ưu truy vấn", IconClass = "bi-database" },
                new Category { Name = "Thiết kế UI/UX", Slug = "thiet-ke-ui-ux", Description = "Tư duy sản phẩm, Design System và giao diện đa thiết bị", IconClass = "bi-palette" },
                new Category { Name = "DevOps & Cloud", Slug = "devops-cloud", Description = "Triển khai ứng dụng, CI/CD và bảo mật hệ thống", IconClass = "bi-cloud-check" }
            );
            await context.SaveChangesAsync();
        }

        // Seed Courses, Chapters, Lessons, Question Bank, Quizzes, Orders, Enrollments, Reviews
        if (!await context.Courses.AnyAsync())
        {
            var catWeb = await context.Categories.FirstAsync(c => c.Slug == "lap-trinh-web");
            var catDb = await context.Categories.FirstAsync(c => c.Slug == "co-so-du-lieu");
            var catUi = await context.Categories.FirstAsync(c => c.Slug == "thiet-ke-ui-ux");

            var course1 = new Course
            {
                Title = "ASP.NET Core 8 MVC Thực Chiến Từ Zero Đến Triển Khai",
                Slug = "aspnet-core-8-mvc-thuc-chien",
                ShortDescription = "Xây dựng ứng dụng web thương mại điện tử và LMS hoàn chỉnh với Identity, EF Core 8, Areas và thanh toán trực tuyến.",
                Description = "Khóa học hướng dẫn từng bước xây dựng hệ thống ASP.NET Core 8 MVC chuẩn doanh nghiệp: cấu trúc Areas, xác thực phân quyền Identity, Entity Framework Core Code First, xử lý nền BackgroundService và tích hợp thanh toán VNPAY/MoMo.",
                ThumbnailUrl = "/images/course-dotnet.svg",
                IntroYoutubeId = "BfEjDD8mWYg",
                Level = "Trung cấp",
                Price = 799000m,
                CategoryId = catWeb.Id,
                InstructorId = instructor.Id,
                Status = CourseStatus.Approved,
                ReviewedByAdminId = admin.Id,
                ReviewedAt = DateTime.UtcNow.AddDays(-10),
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            };

            var course2 = new Course
            {
                Title = "Thiết Kế Giao Diện Responsive Với Bootstrap 5 & Design Tokens",
                Slug = "thiet-ke-responsive-bootstrap-5",
                ShortDescription = "Làm chủ hệ thống lưới responsive, thành phần UI hiện đại, tối ưu trải nghiệm trên điện thoại, máy tính bảng và desktop.",
                Description = "Học cách xây dựng Design System nhất quán với tông màu sáng hiện đại, tối ưu khả năng đọc và truy cập WCAG 2.2, thiết kế dashboard quản trị và trang thương mại khóa học chuyên nghiệp.",
                ThumbnailUrl = "/images/course-uiux.svg",
                IntroYoutubeId = "jy7o8xO1kI0",
                Level = "Cơ bản",
                Price = 499000m,
                CategoryId = catUi.Id,
                InstructorId = instructor.Id,
                Status = CourseStatus.Approved,
                ReviewedByAdminId = admin.Id,
                ReviewedAt = DateTime.UtcNow.AddDays(-8),
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                UpdatedAt = DateTime.UtcNow.AddDays(-8)
            };

            var course3 = new Course
            {
                Title = "Entity Framework Core 8 & Tối Ưu SQL Server Cho Hệ Thống Lớn",
                Slug = "ef-core-8-sql-server-toi-uu",
                ShortDescription = "Thiết kế quan hệ dữ liệu chuẩn, Fluent API, index, migration an toàn và phân tích hiệu năng truy vấn.",
                Description = "Nắm vững cách mô hình hóa thực thể phức tạp, cấu hình unique index, tránh N+1 query, xử lý giao dịch tài chính idempotent và xây dựng báo cáo thống kê hiệu suất cao.",
                ThumbnailUrl = "/images/course-sql.svg",
                IntroYoutubeId = "S8w1lJ3B1qU",
                Level = "Nâng cao",
                Price = 649000m,
                CategoryId = catDb.Id,
                InstructorId = instructor.Id,
                Status = CourseStatus.Approved,
                ReviewedByAdminId = admin.Id,
                ReviewedAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-9),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            };

            var courseDraft = new Course
            {
                Title = "Kiến Trúc Microservices & Bảo Mật API Với .NET 8 (Bản Nháp)",
                Slug = "kien-truc-microservices-net-8-nhap",
                ShortDescription = "Khóa học đang biên soạn về kiến trúc phân tán và bảo mật nâng cao.",
                Description = "Nội dung đang được giảng viên hoàn thiện trước khi gửi duyệt.",
                ThumbnailUrl = "/images/course-microservices.svg",
                Level = "Nâng cao",
                Price = 899000m,
                CategoryId = catWeb.Id,
                InstructorId = instructor.Id,
                Status = CourseStatus.Draft,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            context.Courses.AddRange(course1, course2, course3, courseDraft);
            await context.SaveChangesAsync();

            // Chapters & Lessons for Course 1
            var c1Ch1 = new Chapter { CourseId = course1.Id, Title = "Chương 1: Khởi tạo dự án & Identity", SortOrder = 1 };
            var c1Ch2 = new Chapter { CourseId = course1.Id, Title = "Chương 2: Nghiệp vụ & Thanh toán", SortOrder = 2 };
            context.Chapters.AddRange(c1Ch1, c1Ch2);
            await context.SaveChangesAsync();

            var c1L1 = new Lesson
            {
                ChapterId = c1Ch1.Id,
                Title = "Bài 1: Tổng quan kiến trúc ASP.NET Core MVC & Areas",
                Description = "Giới thiệu cấu trúc thư mục, DI container và cách tách phân vùng Admin/Instructor/Student.",
                LessonType = LessonType.Video,
                SortOrder = 1,
                DurationSeconds = 180,
                IsFreePreview = true,
                YoutubeVideoId = "BfEjDD8mWYg",
                VideoStatus = VideoUploadStatus.Ready
            };
            var c1L2 = new Lesson
            {
                ChapterId = c1Ch1.Id,
                Title = "Bài 2: Tài liệu cấu hình Identity & PasswordOptions",
                Description = "Hướng dẫn chi tiết thiết lập mật khẩu, xác thực email và Google Login.",
                LessonType = LessonType.Document,
                SortOrder = 2,
                DurationSeconds = 120,
                IsFreePreview = false,
                VideoStatus = VideoUploadStatus.None,
                ContentHtml = "<h4>Cấu hình ASP.NET Core Identity</h4><p>1. Bật <code>RequireConfirmedAccount = true</code> để yêu cầu xác thực email.<br/>2. Thiết lập độ dài tối thiểu 8 ký tự, yêu cầu chữ và số.<br/>3. Sử dụng <code>RoleManager</code> để khởi tạo 3 vai trò Admin, Instructor, Student.</p>"
            };
            var c1L3 = new Lesson
            {
                ChapterId = c1Ch2.Id,
                Title = "Bài 3: Xử lý giỏ hàng và chữ ký HMAC VNPAY / MoMo",
                Description = "Triển khai đơn hàng, xác minh chữ ký trả về và xử lý IPN idempotent.",
                LessonType = LessonType.Video,
                SortOrder = 1,
                DurationSeconds = 240,
                IsFreePreview = false,
                YoutubeVideoId = "PkZNo7MFNFg",
                VideoStatus = VideoUploadStatus.Ready
            };
            context.Lessons.AddRange(c1L1, c1L2, c1L3);

            // Chapters & Lessons for Course 2
            var c2Ch1 = new Chapter { CourseId = course2.Id, Title = "Chương 1: Hệ thống màu cam - trắng & Typography", SortOrder = 1 };
            context.Chapters.Add(c2Ch1);
            await context.SaveChangesAsync();

            var c2L1 = new Lesson
            {
                ChapterId = c2Ch1.Id,
                Title = "Bài 1: Thiết kế Design Tokens và độ tương phản chuẩn WCAG",
                Description = "Áp dụng màu cam sáng với nền trắng sạch sẽ, tối ưu hiển thị đa màn hình.",
                LessonType = LessonType.Video,
                SortOrder = 1,
                DurationSeconds = 150,
                IsFreePreview = true,
                YoutubeVideoId = "jy7o8xO1kI0",
                VideoStatus = VideoUploadStatus.Ready
            };
            var c2L2 = new Lesson
            {
                ChapterId = c2Ch1.Id,
                Title = "Bài 2: Tài liệu bố cục Responsive Grid & Offcanvas Dashboard",
                Description = "Hướng dẫn chia cột cho Mobile, Tablet và Desktop với Bootstrap 5.",
                LessonType = LessonType.Document,
                SortOrder = 2,
                DurationSeconds = 120,
                IsFreePreview = false,
                VideoStatus = VideoUploadStatus.None,
                ContentHtml = "<h4>Nguyên tắc Responsive Bootstrap 5</h4><ul><li>Mobile-first với breakpoint <code>sm</code>, <code>md</code>, <code>lg</code>, <code>xl</code>.</li><li>Dùng Offcanvas Sidebar cho màn hình nhỏ ở trang quản trị.</li><li>Đảm bảo mọi bảng dữ liệu nằm trong <code>.table-responsive</code>.</li></ul>"
            };
            context.Lessons.AddRange(c2L1, c2L2);

            // Chapters & Lessons for Course 3
            var c3Ch1 = new Chapter { CourseId = course3.Id, Title = "Chương 1: Fluent API & Index trên SQL Server", SortOrder = 1 };
            context.Chapters.Add(c3Ch1);
            await context.SaveChangesAsync();

            var c3L1 = new Lesson
            {
                ChapterId = c3Ch1.Id,
                Title = "Bài 1: Cấu hình Unique Index và tránh Multiple Cascade Paths",
                Description = "Kinh nghiệm thực tế khi thiết kế khóa ngoại trên SQL Server với EF Core 8.",
                LessonType = LessonType.Video,
                SortOrder = 1,
                DurationSeconds = 200,
                IsFreePreview = true,
                YoutubeVideoId = "S8w1lJ3B1qU",
                VideoStatus = VideoUploadStatus.Ready
            };
            context.Lessons.Add(c3L1);
            await context.SaveChangesAsync();

            // Question Bank & Quizzes for Course 1 & Course 2
            var q1 = new Question
            {
                CourseId = course1.Id,
                Topic = "Identity",
                Content = "Trong ASP.NET Core Identity, thuộc tính nào bắt buộc người dùng phải xác thực email trước khi đăng nhập?",
                OptionA = "RequireConfirmedAccount",
                OptionB = "AllowUnconfirmedLogin",
                OptionC = "SkipEmailCheck",
                OptionD = "AutoConfirmEmail",
                CorrectOption = "A",
                Explanation = "Cấu hình SignInOptions.RequireConfirmedAccount = true yêu cầu tài khoản đã xác thực."
            };
            var q2 = new Question
            {
                CourseId = course1.Id,
                Topic = "Security",
                Content = "Attribute nào trong ASP.NET Core MVC dùng để chống tấn công CSRF trên các action POST?",
                OptionA = "[AllowAnonymous]",
                OptionB = "[ValidateAntiForgeryToken]",
                OptionC = "[HttpGet]",
                OptionD = "[ResponseCache]",
                CorrectOption = "B",
                Explanation = "[ValidateAntiForgeryToken] kiểm tra mã token hợp lệ khi gửi form POST."
            };
            var q3 = new Question
            {
                CourseId = course1.Id,
                Topic = "Payment",
                Content = "Xử lý IPN từ cổng thanh toán cần tính chất gì để không kích hoạt khóa học 2 lần?",
                OptionA = "Idempotent",
                OptionB = "Stateless",
                OptionC = "Recursive",
                OptionD = "Volatile",
                CorrectOption = "A",
                Explanation = "Xử lý idempotent đảm bảo callback lặp lại không tạo giao dịch hoặc kích hoạt trùng."
            };
            var q4 = new Question
            {
                CourseId = course2.Id,
                Topic = "Responsive",
                Content = "Class nào trong Bootstrap 5 giúp bảng dữ liệu cuộn ngang mượt mà trên thiết bị di động?",
                OptionA = ".table-mobile",
                OptionB = ".table-responsive",
                OptionC = ".table-collapse",
                OptionD = ".table-fluid",
                CorrectOption = "B",
                Explanation = ".table-responsive bọc quanh thẻ <table> để cuộn ngang trên màn hình nhỏ."
            };
            var q5 = new Question
            {
                CourseId = course2.Id,
                Topic = "Accessibility",
                Content = "Độ tương phản tối thiểu theo chuẩn WCAG AA cho văn bản nội dung thông thường là bao nhiêu?",
                OptionA = "2.0 : 1",
                OptionB = "3.0 : 1",
                OptionC = "4.5 : 1",
                OptionD = "7.0 : 1",
                CorrectOption = "C",
                Explanation = "Chuẩn WCAG 2.1/2.2 AA yêu cầu tỷ lệ tương phản tối thiểu 4.5:1 cho chữ thường."
            };
            context.Questions.AddRange(q1, q2, q3, q4, q5);
            await context.SaveChangesAsync();

            var quiz1Chapter = new Quiz
            {
                CourseId = course1.Id,
                ChapterId = c1Ch1.Id,
                Title = "Kiểm tra nhanh Chương 1: Identity & Phân quyền",
                Description = "Bài trắc nghiệm củng cố kiến thức chương 1.",
                IsFinalExam = false,
                DurationMinutes = 10,
                PassingScorePercent = 60,
                MaxAttempts = 0
            };
            var quiz1Final = new Quiz
            {
                CourseId = course1.Id,
                ChapterId = null,
                Title = "Bài thi cuối khóa: ASP.NET Core 8 MVC Thực Chiến",
                Description = "Hoàn thành 100% bài học để mở khóa bài thi cuối khóa và nhận chứng chỉ PDF.",
                IsFinalExam = true,
                DurationMinutes = 15,
                PassingScorePercent = 70,
                MaxAttempts = 3
            };
            var quiz2Final = new Quiz
            {
                CourseId = course2.Id,
                ChapterId = null,
                Title = "Bài thi cuối khóa: Thiết Kế Responsive Bootstrap 5",
                Description = "Vượt qua bài thi để nhận chứng chỉ hoàn thành khóa học.",
                IsFinalExam = true,
                DurationMinutes = 10,
                PassingScorePercent = 70,
                MaxAttempts = 3
            };
            context.Quizzes.AddRange(quiz1Chapter, quiz1Final, quiz2Final);
            await context.SaveChangesAsync();

            context.QuizQuestions.AddRange(
                new QuizQuestion { QuizId = quiz1Chapter.Id, QuestionId = q1.Id, SortOrder = 1 },
                new QuizQuestion { QuizId = quiz1Chapter.Id, QuestionId = q2.Id, SortOrder = 2 },
                new QuizQuestion { QuizId = quiz1Final.Id, QuestionId = q1.Id, SortOrder = 1 },
                new QuizQuestion { QuizId = quiz1Final.Id, QuestionId = q2.Id, SortOrder = 2 },
                new QuizQuestion { QuizId = quiz1Final.Id, QuestionId = q3.Id, SortOrder = 3 },
                new QuizQuestion { QuizId = quiz2Final.Id, QuestionId = q4.Id, SortOrder = 1 },
                new QuizQuestion { QuizId = quiz2Final.Id, QuestionId = q5.Id, SortOrder = 2 }
            );
            await context.SaveChangesAsync();

            // Seed a Paid Order + Enrollment + 66.67% Progress + Review for Student on Course 1
            var seedOrder = new Order
            {
                OrderCode = "ORD-SEED-2026001",
                UserId = student.Id,
                TotalAmount = course1.Price,
                Status = OrderStatus.Paid,
                PaymentProvider = PaymentProvider.VNPAY,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                PaidAt = DateTime.UtcNow.AddDays(-4)
            };
            context.Orders.Add(seedOrder);
            await context.SaveChangesAsync();

            var commissionRate = 20.00m;
            var platformAmount = Math.Round(course1.Price * commissionRate / 100m, 2);
            var instructorAmount = course1.Price - platformAmount;

            context.OrderItems.Add(new OrderItem
            {
                OrderId = seedOrder.Id,
                CourseId = course1.Id,
                UnitPrice = course1.Price,
                CommissionRatePercent = commissionRate,
                PlatformAmount = platformAmount,
                InstructorAmount = instructorAmount
            });
            context.Transactions.Add(new Transaction
            {
                OrderId = seedOrder.Id,
                Provider = PaymentProvider.VNPAY,
                TransactionNo = "VNP-SEED-998877",
                Amount = course1.Price,
                Status = TransactionStatus.Success,
                SignatureValid = true,
                RawPayload = "{\"seeded\":true}",
                CreatedAt = DateTime.UtcNow.AddDays(-4)
            });

            var enrollment = new Enrollment
            {
                UserId = student.Id,
                CourseId = course1.Id,
                OrderId = seedOrder.Id,
                EnrolledAt = DateTime.UtcNow.AddDays(-4),
                ProgressPercent = 66.67m
            };
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            context.LessonProgresses.AddRange(
                new LessonProgress
                {
                    EnrollmentId = enrollment.Id,
                    LessonId = c1L1.Id,
                    WatchedSeconds = 175,
                    LastPositionSeconds = 175,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow.AddDays(-3),
                    LastWatchedAt = DateTime.UtcNow.AddDays(-3)
                },
                new LessonProgress
                {
                    EnrollmentId = enrollment.Id,
                    LessonId = c1L2.Id,
                    WatchedSeconds = 120,
                    LastPositionSeconds = 120,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow.AddDays(-2),
                    LastWatchedAt = DateTime.UtcNow.AddDays(-2)
                }
            );

            context.Reviews.Add(new Review
            {
                UserId = student.Id,
                CourseId = course1.Id,
                Rating = 5,
                Comment = "Khóa học rất thực tế, từng bước làm chuẩn MVC, phần thanh toán và Identity giải thích cực kỳ dễ hiểu.",
                IsHidden = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });

            context.CourseQuestions.Add(new CourseQuestion
            {
                CourseId = course1.Id,
                LessonId = c1L3.Id,
                StudentId = student.Id,
                QuestionText = "Thầy ơi khi test VNPAY sandbox trên localhost có cần public URL cho IPN không ạ?",
                CreatedAt = DateTime.UtcNow.AddHours(-6)
            });

            context.Notifications.Add(new Notification
            {
                UserId = instructor.Id,
                Title = "Có câu hỏi mới từ học viên",
                Message = "Trần Bảo Ngọc vừa đặt câu hỏi trong khóa ASP.NET Core 8 MVC Thực Chiến.",
                LinkUrl = "/Instructor/Qna",
                CreatedAt = DateTime.UtcNow.AddHours(-6)
            });

            await context.SaveChangesAsync();
        }

        // Idempotent refresh for both newly initialized and pre-existing LocalDB databases
        await EnsureSupplementalHuitDataAsync(context, userManager, admin, instructor, student);
    }

    private static async Task EnsureSupplementalHuitDataAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ApplicationUser admin,
        ApplicationUser instructor,
        ApplicationUser student)
    {
        if (admin.FullName == "Quản Trị Viên EduSpark" || (admin.FullName != null && admin.FullName.Contains("EduSpark", StringComparison.OrdinalIgnoreCase)))
        {
            admin.FullName = "Quản Trị Viên HUIT Learning";
            admin.Headline = "Quản trị hệ thống đào tạo trực tuyến HUIT Learning";
            await userManager.UpdateAsync(admin);
        }

        var catWeb = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "lap-trinh-web");
        if (catWeb == null)
        {
            catWeb = new Category
            {
                Name = "Lập trình Web",
                Slug = "lap-trinh-web",
                Description = "ASP.NET Core, HTML/CSS, JavaScript và kiến trúc Web hiện đại",
                IconClass = "bi-code-slash"
            };
            context.Categories.Add(catWeb);
            await context.SaveChangesAsync();
        }

        var catDevOps = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "devops-cloud");
        if (catDevOps == null)
        {
            catDevOps = new Category
            {
                Name = "DevOps & Cloud",
                Slug = "devops-cloud",
                Description = "Triển khai ứng dụng, CI/CD và bảo mật hệ thống",
                IconClass = "bi-cloud-check"
            };
            context.Categories.Add(catDevOps);
            await context.SaveChangesAsync();
        }

        const string devOpsSlug = "docker-github-actions-cicd-cloud";
        if (!await context.Courses.AnyAsync(c => c.Slug == devOpsSlug))
        {
            var courseDevOps = new Course
            {
                Title = "Docker, GitHub Actions CI/CD & Triển Khai Cloud Thực Chiến",
                Slug = devOpsSlug,
                ShortDescription = "Đóng gói ứng dụng đa tầng với Docker Compose, tự động hóa kiểm thử và triển khai liên tục lên máy chủ Linux Cloud.",
                Description = "Khóa học thực chiến tại HUIT Learning giúp bạn làm chủ Docker, tối ưu multi-stage Dockerfile cho .NET 8, thiết lập workflow CI/CD tự động với GitHub Actions, cấu hình Reverse Proxy Nginx SSL và giám sát hệ thống sản phẩm thật.",
                ThumbnailUrl = "/images/course-devops-cloud.svg",
                IntroYoutubeId = "3c-iBn73dDE",
                Level = "Trung cấp",
                Price = 699000m,
                CategoryId = catDevOps.Id,
                InstructorId = instructor.Id,
                Status = CourseStatus.Approved,
                ReviewedByAdminId = admin.Id,
                ReviewedAt = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow.AddDays(-6),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            };
            context.Courses.Add(courseDevOps);
            await context.SaveChangesAsync();

            var chDevOps = new Chapter
            {
                CourseId = courseDevOps.Id,
                Title = "Chương 1: Container hóa với Docker & Tự động hóa CI/CD",
                SortOrder = 1
            };
            context.Chapters.Add(chDevOps);
            await context.SaveChangesAsync();

            context.Lessons.AddRange(
                new Lesson
                {
                    ChapterId = chDevOps.Id,
                    Title = "Bài 1: Tối ưu Multi-Stage Dockerfile & Docker Compose cho ASP.NET Core 8",
                    Description = "Đóng gói ứng dụng web cùng SQL Server thành cụm container gọn nhẹ, bảo mật và dễ triển khai.",
                    LessonType = LessonType.Video,
                    SortOrder = 1,
                    DurationSeconds = 210,
                    IsFreePreview = true,
                    YoutubeVideoId = "3c-iBn73dDE",
                    VideoStatus = VideoUploadStatus.Ready
                },
                new Lesson
                {
                    ChapterId = chDevOps.Id,
                    Title = "Bài 2: Thiết lập GitHub Actions Build, Test xUnit & Deploy tự động",
                    Description = "Cấu hình pipeline kiểm thử tự động mỗi khi tạo Pull Request và phát hành không gián đoạn.",
                    LessonType = LessonType.Document,
                    SortOrder = 2,
                    DurationSeconds = 180,
                    IsFreePreview = false,
                    VideoStatus = VideoUploadStatus.None,
                    ContentHtml = "<h4>Chu trình CI/CD chuẩn tại HUIT Learning</h4><ol><li>Tự động chạy <code>dotnet build</code> và <code>dotnet test</code> trên mỗi commit.</li><li>Đóng gói image và đẩy lên GitHub Container Registry (GHCR).</li><li>Kích hoạt triển khai qua SSH với cơ chế healthcheck an toàn.</li></ol>"
                });

            context.Reviews.Add(new Review
            {
                UserId = student.Id,
                CourseId = courseDevOps.Id,
                Rating = 5,
                Comment = "Phần Docker Compose và GitHub Actions hướng dẫn rất trực quan, áp dụng được ngay cho đồ án thực tế.",
                IsHidden = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            });

            await context.SaveChangesAsync();
        }

        const string frontend3dSlug = "threejs-3d-webgl-view-transitions-modern-web";
        if (!await context.Courses.AnyAsync(c => c.Slug == frontend3dSlug))
        {
            var courseFrontend = new Course
            {
                Title = "Lập Trình Web 3D Tương Tác Với Three.js & View Transitions API",
                Slug = frontend3dSlug,
                ShortDescription = "Kiến tạo giao diện Web 3D 60fps bằng Three.js WebGL, hiệu ứng chuyển trang mượt View Transitions và chuẩn truy cập hiện đại.",
                Description = "Khám phá kỹ nghệ Frontend thế hệ mới: dựng cảnh 3D tương tác chuột với Three.js, quản lý hiệu năng render thông minh với IntersectionObserver & ResizeObserver, kết hợp View Transitions API giúp ứng dụng đa trang mượt như Native App.",
                ThumbnailUrl = "/images/course-react-frontend.svg",
                IntroYoutubeId = "PkZNo7MFNFg",
                Level = "Trung cấp",
                Price = 599000m,
                CategoryId = catWeb.Id,
                InstructorId = instructor.Id,
                Status = CourseStatus.Approved,
                ReviewedByAdminId = admin.Id,
                ReviewedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            };
            context.Courses.Add(courseFrontend);
            await context.SaveChangesAsync();

            var chFrontend = new Chapter
            {
                CourseId = courseFrontend.Id,
                Title = "Chương 1: Không gian 3D WebGL & Chuyển động giao diện hiện đại",
                SortOrder = 1
            };
            context.Chapters.Add(chFrontend);
            await context.SaveChangesAsync();

            context.Lessons.AddRange(
                new Lesson
                {
                    ChapterId = chFrontend.Id,
                    Title = "Bài 1: Khởi tạo Scene, Camera, Ánh sáng & Vật thể 3D với Three.js",
                    Description = "Xây dựng khối đa diện tương tác con trỏ chuột mượt 60fps và tự động co giãn theo khung nhìn.",
                    LessonType = LessonType.Video,
                    SortOrder = 1,
                    DurationSeconds = 240,
                    IsFreePreview = true,
                    YoutubeVideoId = "PkZNo7MFNFg",
                    VideoStatus = VideoUploadStatus.Ready
                },
                new Lesson
                {
                    ChapterId = chFrontend.Id,
                    Title = "Bài 2: Tích hợp View Transitions API & Tôn trọng prefers-reduced-motion",
                    Description = "Kỹ thuật morph hình ảnh giữa trang danh mục và chi tiết khóa học không gây giật khung hình.",
                    LessonType = LessonType.Document,
                    SortOrder = 2,
                    DurationSeconds = 150,
                    IsFreePreview = false,
                    VideoStatus = VideoUploadStatus.None,
                    ContentHtml = "<h4>Nguyên tắc Web Motion hiện đại</h4><ul><li>Sử dụng <code>@view-transition { navigation: auto; }</code> và định danh <code>view-transition-name</code> duy nhất cho từng phần tử.</li><li>Tạm dừng vòng lặp <code>requestAnimationFrame</code> khi tab ẩn hoặc khuất tầm nhìn.</li><li>Luôn hỗ trợ chế độ giảm chuyển động <code>prefers-reduced-motion: reduce</code>.</li></ul>"
                });

            context.Reviews.Add(new Review
            {
                UserId = student.Id,
                CourseId = courseFrontend.Id,
                Rating = 5,
                Comment = "Hiệu ứng Three.js và chuyển trang View Transitions cực mượt, tối ưu tốt cả trên điện thoại.",
                IsHidden = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });

            await context.SaveChangesAsync();
        }

        bool hasCourseUpdates = false;
        var draftCourse = await context.Courses.FirstOrDefaultAsync(c => c.Slug == "kien-truc-microservices-net-8-nhap");
        if (draftCourse != null && (string.IsNullOrWhiteSpace(draftCourse.ThumbnailUrl) || draftCourse.ThumbnailUrl == "/images/course-dotnet.svg"))
        {
            draftCourse.ThumbnailUrl = "/images/course-microservices.svg";
            hasCourseUpdates = true;
        }

        var missingThumbCourses = await context.Courses
            .Where(c => c.ThumbnailUrl == null || c.ThumbnailUrl == "")
            .ToListAsync();
        foreach (var course in missingThumbCourses)
        {
            course.ThumbnailUrl = "/images/course-default.svg";
            hasCourseUpdates = true;
        }

        if (hasCourseUpdates)
        {
            await context.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        string headline,
        string bio,
        string[] roles)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Headline = headline,
                Bio = bio,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errs = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Không thể seed tài khoản {email}: {errs}");
            }
        }

        foreach (var role in roles)
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }

        return user;
    }
}
