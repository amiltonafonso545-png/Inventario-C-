using GestorRHPro.Data;
using GestorRHPro.Models;
using GestorRHPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestorRHPro.Controllers;

[Authorize(Roles = "Employee")]
public sealed class EmployeePortalController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public EmployeePortalController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var employee = await GetCurrentEmployeeAsync();
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

        ViewData["Title"] = "Painel do funcionário";
        return View("Dashboard", new EmployeePortalViewModel
        {
            HasEmployeeRecord = true,
            EmployeeName = employee.FullName,
            InternalNumber = employee.InternalNumber,
            Today = DateTime.Today,
            PendingLeaveCount = leaves.Count(item => item.Status == LeaveStatus.Pending),
            CanClockIn = todayAttendance is null,
            CanClockOut = todayAttendance is { CheckIn: not null, CheckOut: null },
            Leaves = leaves,
            AttendanceHistory = attendance,
            PayrollHistory = payroll
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
