using Microsoft.AspNetCore.Identity;
namespace GestorRHPro.Models;
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string Role { get; set; } = "Employee";
    public Employee? Employee { get; set; }
}
