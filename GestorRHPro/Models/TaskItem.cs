using System.ComponentModel.DataAnnotations;

namespace GestorRHPro.Models;

public class TaskItem
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    public WorkItemStatus Status { get; set; } = WorkItemStatus.ToDo;

    public int? AssignedEmployeeId { get; set; }
    public Employee? AssignedEmployee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public enum TaskPriority { Low, Normal, High }
public enum WorkItemStatus { ToDo, InProgress, Done }
