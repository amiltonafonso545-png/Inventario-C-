using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestorRHPro.Data;
using GestorRHPro.Models;
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
    public async Task<IActionResult> Edit(int id, Position p){ if(id!=p.Id) return BadRequest(); if(!ModelState.IsValid){ViewBag.Departments=await _db.Departments.ToListAsync();return View(p);} _db.Update(p); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
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
    public async Task<IActionResult> Create(){ ViewBag.Employees=await _db.Employees.ToListAsync(); return View(); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveRequest l){ if(!ModelState.IsValid){ViewBag.Employees=await _db.Employees.ToListAsync();return View(l);} l.Days=(l.EndDate-l.StartDate).Days+1; _db.LeaveRequests.Add(l); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id){ var l=await _db.LeaveRequests.FindAsync(id); if(l!=null){l.Status=LeaveStatus.Approved; await _db.SaveChangesAsync();} return RedirectToAction(nameof(Index)); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id){ var l=await _db.LeaveRequests.FindAsync(id); if(l!=null){l.Status=LeaveStatus.Rejected; await _db.SaveChangesAsync();} return RedirectToAction(nameof(Index)); }
}
[Authorize(Roles = "Administrator,HRManager")]
public class DocumentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    public DocumentsController(ApplicationDbContext db, IWebHostEnvironment env){_db=db;_env=env;}
    public async Task<IActionResult> Index() => View(await _db.EmployeeDocuments.Include(d=>d.Employee).ToListAsync());
    public async Task<IActionResult> Create(){ ViewBag.Employees=await _db.Employees.ToListAsync(); return View(); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int employeeId, string category, DateTime? expiryDate, IFormFile file)
    {
        if(file==null) return RedirectToAction(nameof(Create));
        var dir=Path.Combine(_env.WebRootPath,"uploads"); Directory.CreateDirectory(dir);
        var name=Guid.NewGuid()+Path.GetExtension(file.FileName);
        var path=Path.Combine(dir,name);
        using(var s=new FileStream(path,FileMode.Create)){ await file.CopyToAsync(s);}
        _db.EmployeeDocuments.Add(new EmployeeDocument{EmployeeId=employeeId,FileName=file.FileName,FilePath="/uploads/"+name,Category=category,ExpiryDate=expiryDate});
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
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
[Authorize(Roles = "Administrator,HRManager")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    public HomeController(ApplicationDbContext db) => _db = db;
    public async Task<IActionResult> Index()
    {
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
