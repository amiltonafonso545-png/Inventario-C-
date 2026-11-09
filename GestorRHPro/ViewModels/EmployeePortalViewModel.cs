using System.ComponentModel.DataAnnotations;
using GestorRHPro.Models;

namespace GestorRHPro.ViewModels;

public sealed class EmployeePortalViewModel
{
    public bool HasEmployeeRecord { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string? InternalNumber { get; init; }
    public DateTime Today { get; init; }
    public int PendingLeaveCount { get; init; }
    public bool CanClockIn { get; init; }
    public bool CanClockOut { get; init; }
    public IReadOnlyList<EmployeeLeaveRow> Leaves { get; init; } = [];
    public IReadOnlyList<EmployeeAttendanceRow> AttendanceHistory { get; init; } = [];
    public IReadOnlyList<EmployeePayrollRow> PayrollHistory { get; init; } = [];
}

public sealed class EmployeeAttendancePageViewModel
{
    public bool HasEmployeeRecord { get; init; }
    public bool CanClockIn { get; init; }
    public bool CanClockOut { get; init; }
    public IReadOnlyList<EmployeeAttendanceRow> AttendanceHistory { get; init; } = [];
}

public sealed class EmployeeLeavesPageViewModel
{
    public bool HasEmployeeRecord { get; init; }
    public IReadOnlyList<EmployeeLeaveRow> Leaves { get; init; } = [];
}

public sealed class EmployeePayrollPageViewModel
{
    public bool HasEmployeeRecord { get; init; }
    public IReadOnlyList<EmployeePayrollRow> PayrollHistory { get; init; } = [];
}

public sealed class EmployeeLeaveRequestInput
{
    [Required(ErrorMessage = "Indique a data de início.")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "Indique a data de fim.")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [StringLength(500, ErrorMessage = "O motivo pode ter até 500 caracteres.")]
    public string? Reason { get; set; }
}

public sealed record EmployeeLeaveRow(DateTime StartDate, DateTime EndDate, int Days, string? Reason, LeaveStatus Status);
public sealed record EmployeeAttendanceRow(DateTime Date, DateTime? CheckIn, DateTime? CheckOut, AttendanceStatus Status);
public sealed record EmployeePayrollRow(int Year, int Month, decimal BaseSalary, decimal Bonuses, decimal Deductions, decimal NetSalary, string? Note);
