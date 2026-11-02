namespace GestorRHPro.Models;
public class Attendance
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime Date { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public DateTime? BreakStart { get; set; }
    public DateTime? BreakEnd { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
}
public enum AttendanceStatus { Present, Absent, Late, OnLeave }
