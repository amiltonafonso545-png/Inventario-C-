using System.ComponentModel.DataAnnotations;
using GestorRHPro.Models;

namespace GestorRHPro.ViewModels;

public sealed class TaskFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O título é obrigatório.")]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    public WorkItemStatus Status { get; set; } = WorkItemStatus.ToDo;

    public int? AssignedEmployeeId { get; set; }
}
