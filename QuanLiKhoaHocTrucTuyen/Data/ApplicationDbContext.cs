using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<InstructorRequest> InstructorRequests => Set<InstructorRequest>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<CommissionConfig> CommissionConfigs => Set<CommissionConfig>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<CourseQuestion> CourseQuestions => Set<CourseQuestion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<InstructorRequest>(e =>
        {
            e.HasOne(x => x.User)
                .WithMany(u => u.InstructorRequests)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ReviewedByAdmin)
                .WithMany()
                .HasForeignKey(x => x.ReviewedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Category>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<Course>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Price).HasColumnType("decimal(18,2)");

            e.HasOne(x => x.Category)
                .WithMany(c => c.Courses)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Instructor)
                .WithMany(u => u.CoursesTeaching)
                .HasForeignKey(x => x.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ReviewedByAdmin)
                .WithMany()
                .HasForeignKey(x => x.ReviewedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Chapter>(e =>
        {
            e.HasOne(x => x.Course)
                .WithMany(c => c.Chapters)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Lesson>(e =>
        {
            e.HasOne(x => x.Chapter)
                .WithMany(c => c.Lessons)
                .HasForeignKey(x => x.ChapterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Attachment>(e =>
        {
            e.HasOne(x => x.Lesson)
                .WithMany(l => l.Attachments)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CartItem>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();

            e.HasOne(x => x.User)
                .WithMany(u => u.CartItems)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Course)
                .WithMany(c => c.CartItems)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Order>(e =>
        {
            e.HasIndex(x => x.OrderCode).IsUnique();
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");

            e.HasOne(x => x.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OrderItem>(e =>
        {
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.CommissionRatePercent).HasColumnType("decimal(5,2)");
            e.Property(x => x.PlatformAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.InstructorAmount).HasColumnType("decimal(18,2)");

            e.HasOne(x => x.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Course)
                .WithMany(c => c.OrderItems)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Transaction>(e =>
        {
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");

            e.HasOne(x => x.Order)
                .WithMany(o => o.Transactions)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Enrollment>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
            e.Property(x => x.ProgressPercent).HasColumnType("decimal(5,2)");

            e.HasOne(x => x.User)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<LessonProgress>(e =>
        {
            e.HasIndex(x => new { x.EnrollmentId, x.LessonId }).IsUnique();

            e.HasOne(x => x.Enrollment)
                .WithMany(en => en.LessonProgresses)
                .HasForeignKey(x => x.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Lesson)
                .WithMany(l => l.LessonProgresses)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Question>(e =>
        {
            e.HasOne(x => x.Course)
                .WithMany(c => c.Questions)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Quiz>(e =>
        {
            e.HasOne(x => x.Course)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Chapter)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(x => x.ChapterId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<QuizQuestion>(e =>
        {
            e.HasKey(x => new { x.QuizId, x.QuestionId });

            e.HasOne(x => x.Quiz)
                .WithMany(q => q.QuizQuestions)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Question)
                .WithMany(q => q.QuizQuestions)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<QuizAttempt>(e =>
        {
            e.Property(x => x.ScorePercent).HasColumnType("decimal(5,2)");

            e.HasOne(x => x.Quiz)
                .WithMany(q => q.Attempts)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.User)
                .WithMany(u => u.QuizAttempts)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Certificate>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();

            e.HasOne(x => x.User)
                .WithMany(u => u.Certificates)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Course)
                .WithMany(c => c.Certificates)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.QuizAttempt)
                .WithMany()
                .HasForeignKey(x => x.QuizAttemptId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Review>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();

            e.HasOne(x => x.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Course)
                .WithMany(c => c.Reviews)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CommissionConfig>(e =>
        {
            e.Property(x => x.PlatformRatePercent).HasColumnType("decimal(5,2)");

            e.HasOne(x => x.UpdatedByAdmin)
                .WithMany()
                .HasForeignKey(x => x.UpdatedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Notification>(e =>
        {
            e.HasOne(x => x.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CourseQuestion>(e =>
        {
            e.HasOne(x => x.Course)
                .WithMany(c => c.QnaQuestions)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Lesson)
                .WithMany(l => l.QnaQuestions)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Student)
                .WithMany(u => u.AskedQuestions)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AnsweredBy)
                .WithMany()
                .HasForeignKey(x => x.AnsweredById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
