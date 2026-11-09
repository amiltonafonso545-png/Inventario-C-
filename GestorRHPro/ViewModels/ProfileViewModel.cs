using System.ComponentModel.DataAnnotations;

namespace GestorRHPro.ViewModels;

public sealed class ProfileViewModel
{
    [Required(ErrorMessage = "Indique o seu nome.")]
    [StringLength(120)]
    [Display(Name = "Nome")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique o seu email.")]
    [EmailAddress(ErrorMessage = "Indique um endereço de email válido.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Indique um número de telefone válido.")]
    [StringLength(32)]
    [Display(Name = "Telefone")]
    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = "Utilizador";
    public string? EmployeeNumber { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public DateTime? HireDate { get; set; }
}
