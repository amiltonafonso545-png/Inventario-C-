namespace GestorRHPro.ViewModels;

public sealed class DashboardViewModel
{
    public DateTime Today { get; init; }
    public int TotalEmployees { get; init; }
    public int ActiveEmployees { get; init; }
    public int InactiveEmployees { get; init; }
    public int NewEmployeesThisMonth { get; init; }
    public int DepartmentsCount { get; init; }
    public int PresentToday { get; init; }
    public int AbsentToday { get; init; }
    public int OnLeaveToday { get; init; }
    public int PendingLeaveRequests { get; init; }
    public decimal CurrentMonthPayroll { get; init; }
    public int PayrollEmployeesThisMonth { get; init; }
    public string[] PayrollMonthLabels { get; init; } = [];
    public decimal[] PayrollMonthTotals { get; init; } = [];
    public int TasksToDo { get; init; }
    public int TasksInProgress { get; init; }
    public int TasksDone { get; init; }
    public int TasksOverdue { get; init; }
    public string[] DepartmentLabels { get; init; } = [];
    public int[] DepartmentValues { get; init; } = [];
    public IReadOnlyList<RecentEmployeeRow> RecentEmployees { get; init; } = [];
    public IReadOnlyList<UpcomingLeaveRow> UpcomingLeaves { get; init; } = [];
    public IReadOnlyList<UpcomingTaskRow> UpcomingTasks { get; init; } = [];
}

public sealed record RecentEmployeeRow(
    int Id,
    string FullName,
    string InternalNumber,
    string? Position,
    string? Department,
    DateTime HireDate,
    string Status);

public sealed record UpcomingLeaveRow(
    string EmployeeName,
    DateTime StartDate,
    DateTime EndDate,
    string Status);

public sealed record UpcomingTaskRow(
    int Id,
    string Title,
    string? EmployeeName,
    DateTime? DueDate,
    string Priority,
    string Status);
