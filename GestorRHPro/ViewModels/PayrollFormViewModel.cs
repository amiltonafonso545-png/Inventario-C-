using System.ComponentModel.DataAnnotations;

namespace GestorRHPro.ViewModels;

public sealed class PayrollFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Selecione um funcionário.")]
    public int EmployeeId { get; set; }

    [Range(2000, 2100, ErrorMessage = "Indique um ano válido.")]
    public int Year { get; set; } = DateTime.Today.Year;

    [Range(1, 12, ErrorMessage = "Selecione um mês válido.")]
    public int Month { get; set; } = DateTime.Today.Month;

    [Range(typeof(decimal), "0", "99999999", ErrorMessage = "O salário base deve ser igual ou superior a zero.")]
    public decimal BaseSalary { get; set; }

    [Range(typeof(decimal), "0", "99999999", ErrorMessage = "Os suplementos devem ser iguais ou superiores a zero.")]
    public decimal Bonuses { get; set; }

    [Range(typeof(decimal), "0", "99999999", ErrorMessage = "As deduções devem ser iguais ou superiores a zero.")]
    public decimal Deductions { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }
}
