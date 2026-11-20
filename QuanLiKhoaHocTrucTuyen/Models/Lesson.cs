using System.ComponentModel.DataAnnotations;

namespace QuanLiKhoaHocTrucTuyen.Models;

public class Lesson
{
    public int Id { get; set; }

    public int ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập tên bài học")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public LessonType LessonType { get; set; } = LessonType.Video;

    public int SortOrder { get; set; } = 1;

    public int DurationSeconds { get; set; }

    public bool IsFreePreview { get; set; }

    [StringLength(500)]
    public string? LocalVideoPath { get; set; }

    [StringLength(100)]
    public string? YoutubeVideoId { get; set; }

    public VideoUploadStatus VideoStatus { get; set; } = VideoUploadStatus.None;

    [StringLength(1000)]
    public string? VideoErrorMessage { get; set; }

    public string? ContentHtml { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    public ICollection<CourseQuestion> QnaQuestions { get; set; } = new List<CourseQuestion>();
}
