namespace QuanLiKhoaHocTrucTuyen.Models;

public enum InstructorRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum LessonType
{
    Video = 0,
    Document = 1
}

public enum VideoUploadStatus
{
    None = 0,
    Processing = 1,
    Ready = 2,
    Error = 3
}

public enum CourseStatus
{
    Draft = 0,
    PendingReview = 1,
    Approved = 2,
    Rejected = 3
}

public enum OrderStatus
{
    PendingPayment = 0,
    Paid = 1,
    Cancelled = 2
}

public enum PaymentProvider
{
    VNPAY = 0,
    MoMo = 1
}

public enum TransactionStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2
}
