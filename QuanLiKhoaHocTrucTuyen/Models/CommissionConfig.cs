using System.ComponentModel.DataAnnotations;

namespace QuanLiKhoaHocTrucTuyen.Models;

public class CommissionConfig
{
    public int Id { get; set; }

    [Range(0, 90, ErrorMessage = "Tỷ lệ chiết khấu nền tảng từ 0% đến 90%")]
    public decimal PlatformRatePercent { get; set; } = 20m;

    [StringLength(500)]
    public string? Note { get; set; }

    public string? UpdatedByAdminId { get; set; }
    public ApplicationUser? UpdatedByAdmin { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
