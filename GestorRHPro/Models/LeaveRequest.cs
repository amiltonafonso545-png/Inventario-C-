namespace GestorRHPro.Models;
public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Days { get; set; }
    public string? Reason { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string? ApprovedById { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
}
public enum LeaveStatus { Pending, Approved, Rejected }
