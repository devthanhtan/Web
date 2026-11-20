using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Areas.Instructor.ViewModels;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Instructor.Controllers;

[Area("Instructor")]
[Authorize(Roles = AppRoles.Instructor)]
public class CoursesController : Controller
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private readonly ApplicationDbContext _db;
    private readonly ICourseManagementService _courseService;
    private readonly IFileStorageService _fileStorage;

    public CoursesController(
        ApplicationDbContext db,
        ICourseManagementService courseService,
        IFileStorageService fileStorage)
    {
        _db = db;
        _courseService = courseService;
        _fileStorage = fileStorage;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var courses = await _courseService.GetInstructorCoursesAsync(CurrentUserId);
        return View(courses);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View(new CourseFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(vm.CategoryId);
            return View(vm);
        }

        string? thumbPath = null;
        if (vm.ThumbnailFile != null && vm.ThumbnailFile.Length > 0)
        {
            var res = await _fileStorage.SaveFileAsync(vm.ThumbnailFile, "thumbnails", ImageExtensions, 5 * 1024 * 1024);
            if (!res.Success)
            {
                ModelState.AddModelError(nameof(vm.ThumbnailFile), res.Error ?? "Ảnh đại diện không hợp lệ.");
                await PopulateCategoriesAsync(vm.CategoryId);
                return View(vm);
            }
            thumbPath = res.RelativePath;
        }

        var course = await _courseService.CreateCourseAsync(
            CurrentUserId, vm.Title, vm.CategoryId, vm.ShortDescription,
            vm.Description, vm.Level, vm.Price, vm.IntroYoutubeId, thumbPath);

        TempData["Success"] = "Đã tạo khóa học mới (Nháp). Hãy thêm chương và bài học.";
        return RedirectToAction(nameof(Curriculum), new { id = course.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var course = await _courseService.GetCourseOwnedByInstructorAsync(id, CurrentUserId);
        if (course == null) return NotFound();

        await PopulateCategoriesAsync(course.CategoryId);
        return View(new CourseFormViewModel
        {
            Id = course.Id,
            Title = course.Title,
            CategoryId = course.CategoryId,
            ShortDescription = course.ShortDescription,
            Description = course.Description,
            Level = course.Level,
            Price = course.Price,
            IntroYoutubeId = course.IntroYoutubeId,
            CurrentThumbnailUrl = course.ThumbnailUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CourseFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(vm.CategoryId);
            return View(vm);
        }

        string? thumbPath = null;
        if (vm.ThumbnailFile != null && vm.ThumbnailFile.Length > 0)
        {
            var res = await _fileStorage.SaveFileAsync(vm.ThumbnailFile, "thumbnails", ImageExtensions, 5 * 1024 * 1024);
            if (!res.Success)
            {
                ModelState.AddModelError(nameof(vm.ThumbnailFile), res.Error ?? "Ảnh đại diện không hợp lệ.");
                await PopulateCategoriesAsync(vm.CategoryId);
                return View(vm);
            }
            thumbPath = res.RelativePath;
        }

        var update = await _courseService.UpdateCourseAsync(
            vm.Id, CurrentUserId, vm.Title, vm.CategoryId, vm.ShortDescription,
            vm.Description, vm.Level, vm.Price, vm.IntroYoutubeId, thumbPath);

        if (!update.Success)
        {
            TempData["Error"] = update.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Đã cập nhật thông tin khóa học.";
        return RedirectToAction(nameof(Curriculum), new { id = vm.Id });
    }

    public async Task<IActionResult> Curriculum(int id)
    {
        var course = await _courseService.GetCourseOwnedByInstructorAsync(id, CurrentUserId);
        if (course == null) return NotFound();
        return View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddChapter(int courseId, string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Vui lòng nhập tên chương.";
            return RedirectToAction(nameof(Curriculum), new { id = courseId });
        }

        var res = await _courseService.AddChapterAsync(courseId, CurrentUserId, title, description);
        TempData[res.Success ? "Success" : "Error"] = res.Success ? "Đã thêm chương mới." : res.Error;
        return RedirectToAction(nameof(Curriculum), new { id = courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateChapter(int chapterId, int courseId, string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Tên chương không được để trống.";
            return RedirectToAction(nameof(Curriculum), new { id = courseId });
        }

        var res = await _courseService.UpdateChapterAsync(chapterId, CurrentUserId, title, description);
        TempData[res.Success ? "Success" : "Error"] = res.Success ? "Đã cập nhật chương." : res.Error;
        return RedirectToAction(nameof(Curriculum), new { id = courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChapter(int chapterId, int courseId)
    {
        var res = await _courseService.DeleteChapterAsync(chapterId, CurrentUserId);
        TempData[res.Success ? "Success" : "Error"] = res.Success ? "Đã xóa chương." : res.Error;
        return RedirectToAction(nameof(Curriculum), new { id = courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveChapter(int chapterId, int courseId, int direction)
    {
        await _courseService.MoveChapterAsync(chapterId, CurrentUserId, direction);
        return RedirectToAction(nameof(Curriculum), new { id = courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLesson(LessonFormViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Title))
        {
            TempData["Error"] = "Vui lòng nhập tên bài học.";
            return RedirectToAction(nameof(Curriculum), new { id = vm.CourseId });
        }

        var res = await _courseService.AddLessonAsync(
            vm.ChapterId, CurrentUserId, vm.Title, vm.Description, vm.LessonType,
            vm.IsFreePreview, vm.DurationSeconds, vm.ContentHtml, vm.VideoFile, vm.AttachmentFile);

        if (!res.Success)
        {
            TempData["Error"] = res.Error;
        }
        else
        {
            TempData["Success"] = vm.LessonType == LessonType.Video
                ? "Đã thêm bài học và đưa video vào hàng đợi tải lên YouTube (đang xử lý nền)."
                : "Đã thêm bài học tài liệu.";
        }

        return RedirectToAction(nameof(Curriculum), new { id = vm.CourseId });
    }

    public async Task<IActionResult> EditLesson(int id)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Chapter).ThenInclude(c => c.Course)
            .Include(l => l.Attachments)
            .FirstOrDefaultAsync(l => l.Id == id && l.Chapter.Course.InstructorId == CurrentUserId);

        if (lesson == null) return NotFound();

        ViewBag.ExistingAttachments = lesson.Attachments.ToList();
        ViewBag.VideoStatus = lesson.VideoStatus;
        ViewBag.YoutubeVideoId = lesson.YoutubeVideoId;
        ViewBag.LocalVideoPath = lesson.LocalVideoPath;
        ViewBag.VideoErrorMessage = lesson.VideoErrorMessage;

        return View(new LessonFormViewModel
        {
            Id = lesson.Id,
            ChapterId = lesson.ChapterId,
            CourseId = lesson.Chapter.CourseId,
            Title = lesson.Title,
            Description = lesson.Description,
            LessonType = lesson.LessonType,
            IsFreePreview = lesson.IsFreePreview,
            DurationSeconds = lesson.DurationSeconds,
            ContentHtml = lesson.ContentHtml
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLesson(LessonFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var current = await _db.Lessons.Include(l => l.Attachments).FirstOrDefaultAsync(l => l.Id == vm.Id);
            ViewBag.ExistingAttachments = current?.Attachments.ToList() ?? [];
            ViewBag.VideoStatus = current?.VideoStatus ?? VideoUploadStatus.None;
            ViewBag.YoutubeVideoId = current?.YoutubeVideoId;
            ViewBag.LocalVideoPath = current?.LocalVideoPath;
            ViewBag.VideoErrorMessage = current?.VideoErrorMessage;
            return View(vm);
        }

        var res = await _courseService.UpdateLessonAsync(
            vm.Id, CurrentUserId, vm.Title, vm.Description, vm.LessonType,
            vm.IsFreePreview, vm.DurationSeconds, vm.ContentHtml, vm.VideoFile, vm.AttachmentFile);

        if (!res.Success)
        {
            TempData["Error"] = res.Error;
            return RedirectToAction(nameof(EditLesson), new { id = vm.Id });
        }

        TempData["Success"] = vm.VideoFile != null
            ? "Đã cập nhật bài học và đưa video mới vào hàng đợi xử lý nền."
            : "Đã cập nhật bài học.";

        return RedirectToAction(nameof(Curriculum), new { id = vm.CourseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLesson(int lessonId, int courseId)
    {
        var res = await _courseService.DeleteLessonAsync(lessonId, CurrentUserId);
        TempData[res.Success ? "Success" : "Error"] = res.Success ? "Đã xóa bài học." : res.Error;
        return RedirectToAction(nameof(Curriculum), new { id = courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveLesson(int lessonId, int courseId, int direction)
    {
        await _courseService.MoveLessonAsync(lessonId, CurrentUserId, direction);
        return RedirectToAction(nameof(Curriculum), new { id = courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttachment(int attachmentId, int lessonId)
    {
        var res = await _courseService.DeleteAttachmentAsync(attachmentId, CurrentUserId);
        TempData[res.Success ? "Success" : "Error"] = res.Success ? "Đã xóa tệp đính kèm." : res.Error;
        return RedirectToAction(nameof(EditLesson), new { id = lessonId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForReview(int courseId)
    {
        var (success, reasons) = await _courseService.SubmitForReviewAsync(courseId, CurrentUserId);
        if (!success)
        {
            TempData["Error"] = "Không thể gửi duyệt: " + string.Join(" | ", reasons);
        }
        else
        {
            TempData["Success"] = "Đã gửi khóa học chờ Admin kiểm duyệt.";
        }
        return RedirectToAction(nameof(Curriculum), new { id = courseId });
    }

    private async Task PopulateCategoriesAsync(int? selectedId = null)
    {
        var categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedId);
    }
}
