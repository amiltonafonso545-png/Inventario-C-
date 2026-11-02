using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GestorRHPro.Models;
namespace GestorRHPro.Data;
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<PayrollRecord> PayrollRecords { get; set; }
    public DbSet<TaskItem> TaskItems { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Employee>().HasOne(e => e.User).WithOne(u => u.Employee).HasForeignKey<Employee>(e => e.UserId);
        builder.Entity<PayrollRecord>()
            .HasIndex(record => new { record.EmployeeId, record.Year, record.Month })
            .IsUnique();
        builder.Entity<PayrollRecord>()
            .HasOne(record => record.Employee)
            .WithMany(employee => employee.PayrollRecords)
            .HasForeignKey(record => record.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TaskItem>()
            .HasOne(task => task.AssignedEmployee)
            .WithMany(employee => employee.AssignedTasks)
            .HasForeignKey(task => task.AssignedEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
