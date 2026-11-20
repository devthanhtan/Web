namespace QuanLiKhoaHocTrucTuyen.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public decimal CommissionRatePercent { get; set; }
    public decimal PlatformAmount { get; set; }
    public decimal InstructorAmount { get; set; }
}
