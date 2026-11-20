namespace QuanLiKhoaHocTrucTuyen.Models;

public class CartItem
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
