using System.ComponentModel.DataAnnotations;

namespace QuanLiKhoaHocTrucTuyen.Models;

public class Transaction
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public PaymentProvider Provider { get; set; }

    [StringLength(100)]
    public string? TransactionNo { get; set; }

    public decimal Amount { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public bool SignatureValid { get; set; }

    public string? RawPayload { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
