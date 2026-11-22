using System.ComponentModel.DataAnnotations;

namespace QuanLiKhoaHocTrucTuyen.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    [StringLength(64)]
    public string OrderCode { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;

    public PaymentProvider PaymentProvider { get; set; } = PaymentProvider.VNPAY;

    /// <summary>vnp_TxnRef của lần thanh toán gần nhất (VNPAY không cho trùng mã trong ngày nên mỗi lần thử lại sinh mã mới).</summary>
    [StringLength(100)]
    public string? PaymentTxnRef { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
