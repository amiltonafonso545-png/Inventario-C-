using GestorRHPro.Data;
using GestorRHPro.Models;
using GestorRHPro.Services;
using GestorRHPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Xml.Linq;

namespace GestorRHPro.Controllers;

[Authorize(Roles = "Employee")]
public sealed class EmployeePortalController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EmployeeDocumentStorage _documentStorage;

    public EmployeePortalController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, EmployeeDocumentStorage documentStorage)
    {
        _db = db;
        _userManager = userManager;
        _documentStorage = documentStorage;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var employee = await _db.Employees.FirstOrDefaultAsync(item => item.UserId == user.Id);
        if (employee is null)
        {
            ViewData["Title"] = "Painel do funcionário";
            return View("Dashboard", new EmployeePortalViewModel { Today = DateTime.Today });
        }

        var today = DateTime.UtcNow.Date;
        var todayAttendance = await _db.Attendances
            .FirstOrDefaultAsync(item => item.EmployeeId == employee.Id && item.Date == today);
        var leaves = await _db.LeaveRequests
            .Where(item => item.EmployeeId == employee.Id)
            .OrderByDescending(item => item.RequestedAt)
            .Take(20)
            .Select(item => new EmployeeLeaveRow(item.StartDate, item.EndDate, item.Days, item.Reason, item.Status))
            .ToListAsync();
        var attendance = await _db.Attendances
            .Where(item => item.EmployeeId == employee.Id)
            .OrderByDescending(item => item.Date)
            .Take(31)
            .Select(item => new EmployeeAttendanceRow(item.Date, item.CheckIn, item.CheckOut, item.Status))
            .ToListAsync();
        var payroll = await _db.PayrollRecords
            .Where(item => item.EmployeeId == employee.Id)
            .OrderByDescending(item => item.Year)
            .ThenByDescending(item => item.Month)
            .Take(12)
            .Select(item => new EmployeePayrollRow(
                item.Year, item.Month, item.BaseSalary, item.Bonuses, item.Deductions,
                item.BaseSalary + item.Bonuses - item.Deductions, item.Note))
            .ToListAsync();
        var documents = await _db.EmployeeDocuments
            .Where(item => item.EmployeeId == employee.Id)
            .OrderByDescending(item => item.UploadedAt)
            .Take(5)
            .Select(item => new EmployeeDocumentRow(item.Id, item.FileName, item.Category, item.UploadedAt, item.ExpiryDate))
            .ToListAsync();
        var notifications = await _db.Notifications
            .Where(item => item.UserId == user.Id)
            .OrderByDescending(item => item.CreatedAt)
            .Take(5)
            .Select(item => new EmployeeNotificationRow(item.Id, item.Title, item.Message, item.CreatedAt, item.IsRead))
            .ToListAsync();
        var tasks = await _db.TaskItems
            .Where(item => item.AssignedEmployeeId == employee.Id)
            .OrderBy(item => item.Status == WorkItemStatus.Done)
            .ThenBy(item => item.DueDate == null)
            .ThenBy(item => item.DueDate)
            .ThenByDescending(item => item.CreatedAt)
            .Take(5)
            .Select(item => new EmployeeTaskRow(item.Id, item.Title, item.Description, item.DueDate, item.Priority, item.Status))
            .ToListAsync();

        ViewData["Title"] = "Painel do funcionário";
        return View("Dashboard", new EmployeePortalViewModel
        {
            HasEmployeeRecord = true,
            EmployeeName = employee.FullName,
            InternalNumber = employee.InternalNumber,
            Today = DateTime.Today,
            PendingLeaveCount = leaves.Count(item => item.Status == LeaveStatus.Pending),
            UnreadNotificationCount = await _db.Notifications.CountAsync(item => item.UserId == user.Id && !item.IsRead),
            CanClockIn = todayAttendance is null,
            CanClockOut = todayAttendance is { CheckIn: not null, CheckOut: null },
            Leaves = leaves,
            AttendanceHistory = attendance,
            PayrollHistory = payroll,
            Documents = documents,
            Notifications = notifications,
            Tasks = tasks
        });
    }

    [HttpGet]
    public async Task<IActionResult> Attendance()
    {
        var employee = await GetCurrentEmployeeAsync();
        if (employee is null)
            return View(new EmployeeAttendancePageViewModel());

        var today = DateTime.UtcNow.Date;
        var todayAttendance = await _db.Attendances
            .FirstOrDefaultAsync(item => item.EmployeeId == employee.Id && item.Date == today);
        var history = await _db.Attendances
            .Where(item => item.EmployeeId == employee.Id)
            .OrderByDescending(item => item.Date)
            .Take(90)
            .Select(item => new EmployeeAttendanceRow(item.Date, item.CheckIn, item.CheckOut, item.Status))
            .ToListAsync();

        return View(new EmployeeAttendancePageViewModel
        {
            HasEmployeeRecord = true,
            CanClockIn = todayAttendance is null,
            CanClockOut = todayAttendance is { CheckIn: not null, CheckOut: null },
            AttendanceHistory = history
        });
    }

    [HttpGet]
    public async Task<IActionResult> Leaves()
    {
        var employee = await GetCurrentEmployeeAsync();
        if (employee is null)
            return View(new EmployeeLeavesPageViewModel());

        var leaves = await _db.LeaveRequests
            .Where(item => item.EmployeeId == employee.Id)
            .OrderByDescending(item => item.RequestedAt)
            .Select(item => new EmployeeLeaveRow(item.StartDate, item.EndDate, item.Days, item.Reason, item.Status))
            .ToListAsync();
        return View(new EmployeeLeavesPageViewModel { HasEmployeeRecord = true, Leaves = leaves });
    }

    [HttpGet]
    public async Task<IActionResult> Payroll()
    {
        var employee = await GetCurrentEmployeeAsync();
        if (employee is null)
            return View(new EmployeePayrollPageViewModel());

        var payroll = await _db.PayrollRecords
            .Where(item => item.EmployeeId == employee.Id)
            .OrderByDescending(item => item.Year)
            .ThenByDescending(item => item.Month)
            .Take(24)
            .Select(item => new EmployeePayrollRow(
                item.Year, item.Month, item.BaseSalary, item.Bonuses, item.Deductions,
                item.BaseSalary + item.Bonuses - item.Deductions, item.Note))
            .ToListAsync();
        return View(new EmployeePayrollPageViewModel { HasEmployeeRecord = true, PayrollHistory = payroll });
    }

    [HttpGet]
    public async Task<IActionResult> Documents()
    {
        var employee = await GetCurrentEmployeeAsync();
        if (employee is null) return View(new EmployeeDocumentsPageViewModel());

        var documents = await _db.EmployeeDocuments
            .Where(item => item.EmployeeId == employee.Id)
            .OrderByDescending(item => item.UploadedAt)
            .Select(item => new EmployeeDocumentRow(item.Id, item.FileName, item.Category, item.UploadedAt, item.ExpiryDate))
            .ToListAsync();
        return View(new EmployeeDocumentsPageViewModel { HasEmployeeRecord = true, Documents = documents });
    }

    [HttpGet]
    public async Task<IActionResult> Tasks()
    {
        var employee = await GetCurrentEmployeeAsync();
        if (employee is null) return View(new EmployeeTasksPageViewModel());

        var tasks = await _db.TaskItems
            .Where(item => item.AssignedEmployeeId == employee.Id)
            .OrderBy(item => item.Status == WorkItemStatus.Done)
            .ThenBy(item => item.DueDate == null)
            .ThenBy(item => item.DueDate)
            .ThenByDescending(item => item.CreatedAt)
            .Select(item => new EmployeeTaskRow(item.Id, item.Title, item.Description, item.DueDate, item.Priority, item.Status))
            .ToListAsync();
        return View(new EmployeeTasksPageViewModel { HasEmployeeRecord = true, Tasks = tasks });
    }

    [HttpGet]
    public async Task<IActionResult> ViewDocument(int id)
    {
        var employee = await GetCurrentEmployeeAsync();
        if (employee is null) return Forbid();

        var document = await _db.EmployeeDocuments.FirstOrDefaultAsync(item => item.Id == id && item.EmployeeId == employee.Id);
        if (document is null) return NotFound();

        var path = _documentStorage.ResolvePath(document.FilePath);
        if (path is null) return NotFound();

        var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
        if (extension == ".docx")
        {
            try
            {
                await using var stream = System.IO.File.OpenRead(path);
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                var content = archive.GetEntry("word/document.xml");
                if (content is null) return BadRequest("O documento Word não contém conteúdo legível.");
                await using var contentStream = content.Open();
                var xml = await XDocument.LoadAsync(contentStream, LoadOptions.None, HttpContext.RequestAborted);
                XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                var paragraphs = xml.Descendants(word + "p")
                    .Select(paragraph => string.Concat(paragraph.Descendants(word + "t").Select(text => text.Value)))
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .ToList();
                return View("DocumentPreview", new EmployeeDocumentPreviewViewModel(document.FileName, paragraphs));
            }
            catch (InvalidDataException)
            {
                return BadRequest("O ficheiro Word está danificado ou não é válido.");
            }
        }

        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".txt" => "text/plain; charset=utf-8",
            ".csv" => "text/csv; charset=utf-8",
            _ => "application/octet-stream"
        };
        Response.Headers["Content-Disposition"] = "inline";
        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        var employee = await GetCurrentEmployeeAsync();
        if (employee is null) return Forbid();

        var document = await _db.EmployeeDocuments.FirstOrDefaultAsync(item => item.Id == id && item.EmployeeId == employee.Id);
        if (document is null) return NotFound();

        var path = _documentStorage.ResolvePath(document.FilePath);
        if (path is null) return NotFound();
        return PhysicalFile(path, "application/octet-stream", Path.GetFileName(document.FileName));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var notification = await _db.Notifications.FirstOrDefaultAsync(item => item.Id == id && item.UserId == user.Id);
        if (notification is not null && !notification.IsRead)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestLeave(EmployeeLeaveRequestInput input)
    {
        var employee = await GetCurrentEmployeeAsync();
        if (employee is null) return Forbid();

        if (input.StartDate.HasValue && input.StartDate.Value.Date < DateTime.Today)
            ModelState.AddModelError(nameof(input.StartDate), "A data de início não pode estar no passado.");
        if (input.StartDate.HasValue && input.EndDate.HasValue && input.EndDate.Value.Date < input.StartDate.Value.Date)
            ModelState.AddModelError(nameof(input.EndDate), "A data de fim deve ser igual ou posterior à data de início.");

        if (ModelState.IsValid)
        {
            var start = input.StartDate!.Value.Date;
            var end = input.EndDate!.Value.Date;
            var overlaps = await _db.LeaveRequests.AnyAsync(item => item.EmployeeId == employee.Id
                && item.Status != LeaveStatus.Rejected
                && item.StartDate <= end && item.EndDate >= start);
            if (overlaps)
                ModelState.AddModelError(string.Empty, "Já existe um pedido pendente ou aprovado que coincide com essas datas.");
        }

        if (!ModelState.IsValid)
        {
            TempData["PortalError"] = string.Join(" ", ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage));
            return RedirectToAction(nameof(Leaves));
        }

        var startDate = input.StartDate!.Value.Date;
        var endDate = input.EndDate!.Value.Date;
        _db.LeaveRequests.Add(new LeaveRequest
        {
            EmployeeId = employee.Id,
            StartDate = startDate,
            EndDate = endDate,
            Days = (endDate - startDate).Days + 1,
            Reason = input.Reason?.Trim(),
            Status = LeaveStatus.Pending,
            RequestedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["PortalMessage"] = "O pedido de férias foi enviado para aprovação.";
        return RedirectToAction(nameof(Leaves));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ClockIn()
    {
        var employee = await GetCurrentEmployeeAsync();
        if (employee is null) return Forbid();

        var today = DateTime.UtcNow.Date;
        var record = await _db.Attendances.FirstOrDefaultAsync(item => item.EmployeeId == employee.Id && item.Date == today);
        if (record is not null)
        {
            TempData["PortalError"] = "Já existe um registo de assiduidade para hoje.";
            return RedirectToAction(nameof(Attendance));
        }

        var now = DateTime.UtcNow;
        _db.Attendances.Add(new Attendance
        {
            EmployeeId = employee.Id,
            Date = today,
            CheckIn = now,
            Status = AttendanceStatus.Present
        });
        await _db.SaveChangesAsync();
        TempData["PortalMessage"] = "Entrada registada.";
            return RedirectToAction(nameof(Attendance));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ClockOut()
    {
        var employee = await GetCurrentEmployeeAsync();
        if (employee is null) return Forbid();

        var today = DateTime.UtcNow.Date;
        var record = await _db.Attendances.FirstOrDefaultAsync(item => item.EmployeeId == employee.Id && item.Date == today);
        if (record is null || record.CheckIn is null || record.CheckOut is not null)
        {
            TempData["PortalError"] = "Não há uma entrada aberta para terminar hoje.";
            return RedirectToAction(nameof(Attendance));
        }

        record.CheckOut = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["PortalMessage"] = "Saída registada.";
        return RedirectToAction(nameof(Attendance));
    }

    private async Task<Employee?> GetCurrentEmployeeAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return null;
        return await _db.Employees.FirstOrDefaultAsync(employee => employee.UserId == user.Id);
    }
}
