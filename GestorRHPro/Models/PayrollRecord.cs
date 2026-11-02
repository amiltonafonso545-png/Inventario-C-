using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestorRHPro.Models;

public class PayrollRecord
{
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    [Range(2000, 2100)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }

    [Range(typeof(decimal), "0", "99999999")]
    [Column(TypeName = "decimal(12,2)")]
    public decimal BaseSalary { get; set; }

    [Range(typeof(decimal), "0", "99999999")]
    [Column(TypeName = "decimal(12,2)")]
    public decimal Bonuses { get; set; }

    [Range(typeof(decimal), "0", "99999999")]
    [Column(TypeName = "decimal(12,2)")]
    public decimal Deductions { get; set; }

    [MaxLength(300)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public decimal NetSalary => BaseSalary + Bonuses - Deductions;
}
