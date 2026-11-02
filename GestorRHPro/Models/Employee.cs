namespace GestorRHPro.Models;
public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public string InternalNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int? PositionId { get; set; }
    public Position? Position { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public string? EmergencyContact { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();
    public ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
}
public enum EmployeeStatus { Active, Inactive, OnLeave }
