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
public class QuestionsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IQuestionImportService _importService;

    public QuestionsController(ApplicationDbContext db, IQuestionImportService importService)
    {
        _db = db;
        _importService = importService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(int? courseId)
    {
        var courses = await _db.Courses
            .Where(c => c.InstructorId == CurrentUserId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        var selectedCourseId = courseId ?? courses.FirstOrDefault()?.Id ?? 0;
        ViewBag.Courses = new SelectList(courses, "Id", "Title", selectedCourseId);
        ViewBag.SelectedCourseId = selectedCourseId;

        var questions = selectedCourseId == 0
            ? new List<Question>()
            : await _db.Questions
                .Where(q => q.CourseId == selectedCourseId && q.Course.InstructorId == CurrentUserId)
                .OrderByDescending(q => q.Id)
                .ToListAsync();

        return View(questions);
    }

    public async Task<IActionResult> Create(int courseId)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorId == CurrentUserId);
        if (course == null) return NotFound();

        ViewBag.CourseTitle = course.Title;
        return View(new QuestionFormViewModel { CourseId = courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuestionFormViewModel vm)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == vm.CourseId && c.InstructorId == CurrentUserId);
        if (course == null) return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.CourseTitle = course.Title;
            return View(vm);
        }

        _db.Questions.Add(new Question
        {
            CourseId = vm.CourseId,
            Topic = vm.Topic?.Trim(),
            Content = vm.Content.Trim(),
            OptionA = vm.OptionA.Trim(),
            OptionB = vm.OptionB.Trim(),
            OptionC = vm.OptionC.Trim(),
            OptionD = vm.OptionD.Trim(),
            CorrectOption = vm.CorrectOption.ToUpperInvariant(),
            Explanation = vm.Explanation?.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã thêm câu hỏi vào ngân hàng.";
        return RedirectToAction(nameof(Index), new { courseId = vm.CourseId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var q = await _db.Questions
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == id && x.Course.InstructorId == CurrentUserId);
        if (q == null) return NotFound();

        ViewBag.CourseTitle = q.Course.Title;
        return View(new QuestionFormViewModel
        {
            Id = q.Id,
            CourseId = q.CourseId,
            Topic = q.Topic,
            Content = q.Content,
            OptionA = q.OptionA,
            OptionB = q.OptionB,
            OptionC = q.OptionC,
            OptionD = q.OptionD,
            CorrectOption = q.CorrectOption,
            Explanation = q.Explanation
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(QuestionFormViewModel vm)
    {
        var q = await _db.Questions
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == vm.Id && x.Course.InstructorId == CurrentUserId);
        if (q == null) return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.CourseTitle = q.Course.Title;
            return View(vm);
        }

        q.Topic = vm.Topic?.Trim();
        q.Content = vm.Content.Trim();
        q.OptionA = vm.OptionA.Trim();
        q.OptionB = vm.OptionB.Trim();
        q.OptionC = vm.OptionC.Trim();
        q.OptionD = vm.OptionD.Trim();
        q.CorrectOption = vm.CorrectOption.ToUpperInvariant();
        q.Explanation = vm.Explanation?.Trim();
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật câu hỏi.";
        return RedirectToAction(nameof(Index), new { courseId = q.CourseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var q = await _db.Questions
            .Include(x => x.Course)
            .Include(x => x.QuizQuestions)
            .FirstOrDefaultAsync(x => x.Id == id && x.Course.InstructorId == CurrentUserId);
        if (q == null) return Forbid();

        var courseId = q.CourseId;
        _db.QuizQuestions.RemoveRange(q.QuizQuestions);
        _db.Questions.Remove(q);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xóa câu hỏi.";
        return RedirectToAction(nameof(Index), new { courseId });
    }

    public async Task<IActionResult> Import(int courseId)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorId == CurrentUserId);
        if (course == null) return NotFound();

        return View(new QuestionImportViewModel
        {
            CourseId = course.Id,
            CourseTitle = course.Title
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(QuestionImportViewModel vm)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == vm.CourseId && c.InstructorId == CurrentUserId);
        if (course == null) return Forbid();

        vm.CourseTitle = course.Title;
        if (vm.File == null || vm.File.Length == 0)
        {
            ModelState.AddModelError(nameof(vm.File), "Vui lòng chọn tệp .xlsx hoặc .csv.");
            return View(vm);
        }

        var ext = Path.GetExtension(vm.File.FileName);
        await using var stream = vm.File.OpenReadStream();
        var result = await _importService.ImportFromStreamAsync(course.Id, stream, ext);

        if (!result.Success)
        {
            vm.RowErrors = result.RowErrors;
            TempData["Error"] = "Phát hiện lỗi định dạng trong tệp tải lên. Vui lòng xem chi tiết bên dưới.";
            return View(vm);
        }

        TempData["Success"] = $"Đã nhập thành công {result.ImportedCount} câu hỏi vào ngân hàng.";
        return RedirectToAction(nameof(Index), new { courseId = course.Id });
    }

    public IActionResult DownloadTemplate()
    {
        var bytes = _importService.GenerateExcelTemplate();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Mau_Nhap_Cau_Hoi_EduSpark.xlsx");
    }
}
