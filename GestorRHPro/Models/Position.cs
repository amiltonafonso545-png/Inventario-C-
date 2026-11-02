namespace GestorRHPro.Models;
public class Position
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
