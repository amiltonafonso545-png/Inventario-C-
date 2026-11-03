using GestorRHPro.Data;
using GestorRHPro.Models;
using GestorRHPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestorRHPro.Controllers;

[Authorize]
public sealed class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ProfileController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var model = await BuildViewModelAsync(user);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel input)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (!ModelState.IsValid)
        {
            await AddEmployeeDetailsAsync(input, user.Id);
            return View(input);
        }

        var email = input.Email.Trim();
        var emailChanged = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        user.FullName = input.FullName.Trim();
        user.Email = email;
        user.UserName = email;
        user.PhoneNumber = input.PhoneNumber?.Trim();
        if (emailChanged) user.EmailConfirmed = false;

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            await AddEmployeeDetailsAsync(input, user.Id);
            return View(input);
        }

        var employee = await _db.Employees.FirstOrDefaultAsync(item => item.UserId == user.Id);
        if (employee is not null)
        {
            employee.Email = email;
            employee.Phone = input.PhoneNumber?.Trim();
            await _db.SaveChangesAsync();
        }
        await transaction.CommitAsync();

        await _signInManager.RefreshSignInAsync(user);
        TempData["ProfileSuccess"] = "O perfil foi atualizado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfileViewModel> BuildViewModelAsync(ApplicationUser user)
    {
        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
        var model = new ProfileViewModel
        {
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = role switch
            {
                "Administrator" => "Administrador",
                "HRManager" => "Recursos Humanos",
                "Employee" => "Funcionário",
                _ => "Utilizador"
            }
        };

        await AddEmployeeDetailsAsync(model, user.Id);
        return model;
    }

    private async Task AddEmployeeDetailsAsync(ProfileViewModel model, string userId)
    {
        var employee = await _db.Employees
            .Include(item => item.Department)
            .Include(item => item.Position)
            .FirstOrDefaultAsync(item => item.UserId == userId);

        if (employee is null) return;
        model.EmployeeNumber = employee.InternalNumber;
        model.Department = employee.Department?.Name;
        model.Position = employee.Position?.Name;
        model.HireDate = employee.HireDate;
    }
}
