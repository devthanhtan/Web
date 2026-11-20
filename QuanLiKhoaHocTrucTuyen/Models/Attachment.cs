using System.ComponentModel.DataAnnotations;

namespace QuanLiKhoaHocTrucTuyen.Models;

public class Attachment
{
    public int Id { get; set; }

    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    [StringLength(150)]
    public string ContentType { get; set; } = "application/octet-stream";

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
