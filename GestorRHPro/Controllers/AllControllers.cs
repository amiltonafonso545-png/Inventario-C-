using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestorRHPro.Data;
using GestorRHPro.Models;
using GestorRHPro.Services;
using GestorRHPro.ViewModels;
using System.Globalization;
namespace GestorRHPro.Controllers;
[Authorize(Roles = "Administrator,HRManager")]
public class PositionsController : Controller
{
    private readonly ApplicationDbContext _db;
    public PositionsController(ApplicationDbContext db) => _db = db;
    public async Task<IActionResult> Index() => View(await _db.Positions.Include(p=>p.Department).ToListAsync());
    public async Task<IActionResult> Create(){ ViewBag.Departments=await _db.Departments.ToListAsync(); return View(); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Position p){ if(!ModelState.IsValid){ViewBag.Departments=await _db.Departments.ToListAsync();return View(p);} _db.Positions.Add(p); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Edit(int id){ var p=await _db.Positions.FindAsync(id); if(p==null) return NotFound(); ViewBag.Departments=await _db.Departments.ToListAsync(); return View(p); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Position p)
    {
        if (id != p.Id) return BadRequest();
        if (p.DepartmentId.HasValue && !await _db.Departments.AnyAsync(department => department.Id == p.DepartmentId.Value))
            ModelState.AddModelError(nameof(p.DepartmentId), "Selecione um departamento válido.");

        if (!ModelState.IsValid)
        {
            ViewBag.Departments = await _db.Departments.OrderBy(department => department.Name).ToListAsync();
            return View(p);
        }

        var position = await _db.Positions.FindAsync(id);
        if (position is null) return NotFound();
        position.Name = p.Name.Trim();
        position.Description = p.Description?.Trim();
        position.DepartmentId = p.DepartmentId;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Delete(int id){ var p=await _db.Positions.FindAsync(id); if(p==null) return NotFound(); return View(p); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id){ var p=await _db.Positions.FindAsync(id); if(p!=null){_db.Positions.Remove(p); await _db.SaveChangesAsync();} return RedirectToAction(nameof(Index)); }
}
[Authorize(Roles = "Administrator,HRManager")]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _db;
    public AttendanceController(ApplicationDbContext db) => _db = db;
    public async Task<IActionResult> Index() => View(await _db.Attendances.Include(a=>a.Employee).OrderByDescending(a=>a.Date).ToListAsync());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int employeeId){ _db.Attendances.Add(new Attendance{EmployeeId=employeeId,Date=DateTime.UtcNow.Date,CheckIn=DateTime.UtcNow,Status=AttendanceStatus.Present}); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(int id){ var a=await _db.Attendances.FindAsync(id); if(a!=null){a.CheckOut=DateTime.UtcNow; await _db.SaveChangesAsync();} return RedirectToAction(nameof(Index)); }
}
[Authorize(Roles = "Administrator,HRManager")]
public class LeaveRequestsController : Controller
{
    private readonly ApplicationDbContext _db;
    public LeaveRequestsController(ApplicationDbContext db) => _db = db;
    public async Task<IActionResult> Index() => View(await _db.LeaveRequests.Include(l=>l.Employee).OrderByDescending(l=>l.RequestedAt).ToListAsync());
    public async Task<IActionResult> Create(){ await LoadEmployeesAsync(); return View(); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int? employeeId, DateTime? startDate, DateTime? endDate, string? reason)
    {
        var l = new LeaveRequest
        {
            EmployeeId = employeeId ?? 0,
            StartDate = startDate ?? default,
            EndDate = endDate ?? default,
            Reason = reason
        };

        if (!employeeId.HasValue || !await _db.Employees.AnyAsync(employee => employee.Id == employeeId.Value))
            ModelState.AddModelError("EmployeeId", "Selecione um funcionário válido.");
        if (!startDate.HasValue)
            ModelState.AddModelError("StartDate", "Indique a data de início.");
        if (!endDate.HasValue)
            ModelState.AddModelError("EndDate", "Indique a data de fim.");
        if (startDate.HasValue && endDate.HasValue && endDate.Value.Date < startDate.Value.Date)
            ModelState.AddModelError("EndDate", "A data de fim deve ser igual ou posterior à data de início.");

        if (!ModelState.IsValid)
        {
            await LoadEmployeesAsync();
            return View(l);
        }

        l.Days = (l.EndDate.Date - l.StartDate.Date).Days + 1;
        l.Status = LeaveStatus.Pending;
        l.RequestedAt = DateTime.UtcNow;
        _db.LeaveRequests.Add(l);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadEmployeesAsync() => ViewBag.Employees = await _db.Employees.OrderBy(employee => employee.FullName).ToListAsync();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id){ var l=await _db.LeaveRequests.FindAsync(id); if(l!=null){l.Status=LeaveStatus.Approved; await _db.SaveChangesAsync();} return RedirectToAction(nameof(Index)); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id){ var l=await _db.LeaveRequests.FindAsync(id); if(l!=null){l.Status=LeaveStatus.Rejected; await _db.SaveChangesAsync();} return RedirectToAction(nameof(Index)); }
}
[Authorize(Roles = "Administrator,HRManager")]
public class DocumentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly EmployeeDocumentStorage _storage;
    public DocumentsController(ApplicationDbContext db, EmployeeDocumentStorage storage){_db=db;_storage=storage;}
    public async Task<IActionResult> Index() => View(await _db.EmployeeDocuments.Include(d=>d.Employee).ToListAsync());
    public async Task<IActionResult> Download(int id)
    {
        var document = await _db.EmployeeDocuments.FindAsync(id);
        if (document is null) return NotFound();
        var path = _storage.ResolvePath(document.FilePath);
        if (path is null) return NotFound();
        return PhysicalFile(path, "application/octet-stream", Path.GetFileName(document.FileName));
    }
    public async Task<IActionResult> Create(){ ViewBag.Employees=await _db.Employees.ToListAsync(); return View(); }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(16_000_000)]
    public async Task<IActionResult> Create(int employeeId, string? category, DateTime? expiryDate, IFormFile? file)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(item => item.Id == employeeId);
        if (employee is null || file is null || file.Length == 0)
        {
            TempData["DocumentNotice"] = employee is null
                ? "Selecione um funcionário válido."
                : "Selecione um ficheiro para enviar.";
            return RedirectToAction(nameof(Create));
        }
        const long maxFileSize = 15 * 1024 * 1024;
        var extension = await GetSupportedExtensionAsync(file);
        if (file.Length > maxFileSize || extension is null)
        {
            TempData["DocumentNotice"] = file.Length > maxFileSize
                ? "O ficheiro ultrapassa o limite de 15 MB."
                : "Formato não suportado ou conteúdo inválido. Use PDF, DOC, DOCX, XLS, XLSX, JPG ou PNG.";
            return RedirectToAction(nameof(Create));
        }

        var storageKey = await _storage.SaveAsync(file, extension);
        var displayName = Path.GetFileName(file.FileName);
        var document = new EmployeeDocument
        {
            EmployeeId = employeeId,
            FileName = displayName,
            FilePath = storageKey,
            Category = string.IsNullOrWhiteSpace(category) ? "Outros" : category.Trim(),
            ExpiryDate = expiryDate,
            UploadedAt = DateTime.UtcNow
        };
        _db.EmployeeDocuments.Add(document);
        if (!string.IsNullOrWhiteSpace(employee.UserId))
        {
            _db.Notifications.Add(new Notification
            {
                UserId = employee.UserId,
                Title = "Novo documento disponível",
                Message = $"O documento {displayName} foi adicionado à sua área pessoal.",
                CreatedAt = DateTime.UtcNow
            });
        }
        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            _storage.Delete(storageKey);
            throw;
        }
        TempData["DocumentNotice"] = string.IsNullOrWhiteSpace(employee.UserId)
            ? "Documento guardado, mas o funcionário ainda não tem uma conta associada e não recebeu notificação."
            : "Documento guardado e notificação enviada ao funcionário.";
        return RedirectToAction(nameof(Index));
    }

    private static async Task<string?> GetSupportedExtensionAsync(IFormFile file)
    {
        var extension = Path.GetExtension(Path.GetFileName(file.FileName)).ToLowerInvariant();
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header);
        var isSupported = extension switch
        {
            ".pdf" => bytesRead >= 5 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46 && header[4] == 0x2D,
            ".jpg" or ".jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".docx" or ".xlsx" => bytesRead >= 4 && header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04,
            ".doc" or ".xls" => bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
            _ => false
        };
        return isSupported ? extension : null;
    }
}
[Authorize(Roles = "Administrator,HRManager")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ReportsController(ApplicationDbContext db) => _db = db;
    public async Task<IActionResult> Index()
    {
        ViewBag.Employees = await _db.Employees.CountAsync();
        ViewBag.Departments = await _db.Departments.CountAsync();
        ViewBag.Leaves = await _db.LeaveRequests.CountAsync(l=>l.Status==LeaveStatus.Pending);
        return View();
    }
}
[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    public HomeController(ApplicationDbContext db) => _db = db;
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Employee"))
            return RedirectToAction("Index", "EmployeePortal");
        if (!User.IsInRole("Administrator") && !User.IsInRole("HRManager"))
            return Forbid();

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var chartStart = monthStart.AddMonths(-11);

        var payrollRows = await _db.PayrollRecords
            .Where(record => (record.Year > chartStart.Year || (record.Year == chartStart.Year && record.Month >= chartStart.Month))
                && (record.Year < monthEnd.Year || (record.Year == monthEnd.Year && record.Month < monthEnd.Month)))
            .Select(record => new { record.EmployeeId, record.Year, record.Month, record.BaseSalary, record.Bonuses, record.Deductions })
            .ToListAsync();

        var chartMonths = Enumerable.Range(0, 12)
            .Select(offset => chartStart.AddMonths(offset))
            .ToArray();
        var payrollTotalsByMonth = chartMonths.Select(month => payrollRows
                .Where(row => row.Year == month.Year && row.Month == month.Month)
                .Sum(row => row.BaseSalary + row.Bonuses - row.Deductions))
            .ToArray();

        var upcomingTasks = await _db.TaskItems
            .Where(task => task.Status != WorkItemStatus.Done && task.DueDate != null)
            .OrderBy(task => task.DueDate)
            .Take(4)
            .Select(task => new UpcomingTaskRow(
                task.Id,
                task.Title,
                task.AssignedEmployee == null ? null : task.AssignedEmployee.FullName,
                task.DueDate,
                task.Priority.ToString(),
                task.Status.ToString()))
            .ToListAsync();

        var departments = await _db.Departments
            .Select(department => new { department.Name, Employees = department.Employees.Count })
            .OrderByDescending(department => department.Employees)
            .ThenBy(department => department.Name)
            .ToListAsync();

        var recentEmployees = await _db.Employees
            .OrderByDescending(employee => employee.HireDate)
            .ThenByDescending(employee => employee.Id)
            .Take(6)
            .Select(employee => new RecentEmployeeRow(
                employee.Id,
                employee.FullName,
                employee.InternalNumber,
                employee.Position == null ? null : employee.Position.Name,
                employee.Department == null ? null : employee.Department.Name,
                employee.HireDate,
                employee.Status.ToString()))
            .ToListAsync();

        var upcomingLeaves = await _db.LeaveRequests
            .Where(leave => leave.Status == LeaveStatus.Pending
                || (leave.Status == LeaveStatus.Approved && leave.EndDate >= today))
            .OrderBy(leave => leave.StartDate)
            .Take(5)
            .Select(leave => new UpcomingLeaveRow(
                leave.Employee.FullName,
                leave.StartDate,
                leave.EndDate,
                leave.Status.ToString()))
            .ToListAsync();

        var model = new DashboardViewModel
        {
            Today = today,
            TotalEmployees = await _db.Employees.CountAsync(),
            ActiveEmployees = await _db.Employees.CountAsync(employee => employee.Status == EmployeeStatus.Active),
            InactiveEmployees = await _db.Employees.CountAsync(employee => employee.Status == EmployeeStatus.Inactive),
            NewEmployeesThisMonth = await _db.Employees.CountAsync(employee => employee.HireDate >= monthStart && employee.HireDate < monthEnd),
            DepartmentsCount = departments.Count,
            PresentToday = await _db.Attendances.CountAsync(attendance => attendance.Date >= today && attendance.Date < tomorrow && attendance.CheckIn != null),
            AbsentToday = await _db.Attendances.CountAsync(attendance => attendance.Date >= today && attendance.Date < tomorrow && attendance.Status == AttendanceStatus.Absent),
            OnLeaveToday = await _db.LeaveRequests.CountAsync(leave => leave.Status == LeaveStatus.Approved && leave.StartDate < tomorrow && leave.EndDate >= today),
            PendingLeaveRequests = await _db.LeaveRequests.CountAsync(leave => leave.Status == LeaveStatus.Pending),
            CurrentMonthPayroll = payrollRows.Where(row => row.Year == today.Year && row.Month == today.Month).Sum(row => row.BaseSalary + row.Bonuses - row.Deductions),
            PayrollEmployeesThisMonth = payrollRows.Where(row => row.Year == today.Year && row.Month == today.Month).Select(row => row.EmployeeId).Distinct().Count(),
            PayrollMonthLabels = chartMonths.Select(month => CultureInfo.GetCultureInfo("pt-PT").TextInfo.ToTitleCase(month.ToString("MMM", CultureInfo.GetCultureInfo("pt-PT")))).ToArray(),
            PayrollMonthTotals = payrollTotalsByMonth,
            TasksToDo = await _db.TaskItems.CountAsync(task => task.Status == WorkItemStatus.ToDo),
            TasksInProgress = await _db.TaskItems.CountAsync(task => task.Status == WorkItemStatus.InProgress),
            TasksDone = await _db.TaskItems.CountAsync(task => task.Status == WorkItemStatus.Done),
            TasksOverdue = await _db.TaskItems.CountAsync(task => task.Status != WorkItemStatus.Done && task.DueDate != null && task.DueDate < today),
            DepartmentLabels = departments.Select(department => department.Name).ToArray(),
            DepartmentValues = departments.Select(department => department.Employees).ToArray(),
            RecentEmployees = recentEmployees,
            UpcomingLeaves = upcomingLeaves,
            UpcomingTasks = upcomingTasks
        };

        return View(model);
    }
    public IActionResult Privacy() => View();
}
