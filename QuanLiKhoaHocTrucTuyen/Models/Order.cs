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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
