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
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public ProfileController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _environment = environment;
        _configuration = configuration;
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

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6_000_000)]
    public async Task<IActionResult> UploadPhoto(IFormFile? photo)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (photo is null || photo.Length == 0)
            TempData["ProfileError"] = "Escolha uma fotografia para carregar.";
        else if (photo.Length > 5 * 1024 * 1024)
            TempData["ProfileError"] = "A fotografia não pode ultrapassar 5 MB.";
        else
        {
            var extension = await GetImageExtensionAsync(photo);
            if (extension is null)
                TempData["ProfileError"] = "Use uma imagem JPG, PNG ou WebP válida.";
            else
            {
                var directory = GetPhotoDirectory();
                Directory.CreateDirectory(directory);
                var destination = Path.Combine(directory, user.Id + extension);
                var temporary = Path.Combine(directory, user.Id + "." + Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
                        await photo.CopyToAsync(stream);
                    System.IO.File.Move(temporary, destination, overwrite: true);
                    foreach (var oldPhoto in Directory.EnumerateFiles(directory, user.Id + ".*"))
                        if (!string.Equals(oldPhoto, destination, StringComparison.OrdinalIgnoreCase))
                            System.IO.File.Delete(oldPhoto);
                    TempData["ProfileSuccess"] = "A fotografia do perfil foi atualizada.";
                }
                finally
                {
                    if (System.IO.File.Exists(temporary)) System.IO.File.Delete(temporary);
                }
            }
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Photo()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";

        var directory = GetPhotoDirectory();
        foreach (var (extension, contentType) in new[]
        {
            (".jpg", "image/jpeg"), (".png", "image/png"), (".webp", "image/webp")
        })
        {
            var path = Path.Combine(directory, user.Id + extension);
            if (System.IO.File.Exists(path)) return PhysicalFile(path, contentType);
        }
        return NotFound();
    }

    private string GetPhotoDirectory() => _configuration["Storage:ProfilePhotosPath"]
        ?? Path.Combine(_environment.ContentRootPath, "App_Data", "ProfilePhotos");

    private static async Task<string?> GetImageExtensionAsync(IFormFile photo)
    {
        var header = new byte[12];
        await using var stream = photo.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header);
        if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return ".jpg";
        if (bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";
        if (bytesRead >= 12
            && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            && header.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }

    private async Task<ProfileViewModel> BuildViewModelAsync(ApplicationUser user)
    {
        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
        var model = new ProfileViewModel
        {
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            Role = role switch
            {
                "Administrator" => "Administrador",
                "HRManager" => "Recursos Humanos",
                "Employee" => "Funcionário",
                _ => "Utilizador"
            },
            HasPhoto = GetPhotoExtensions().Any(extension => System.IO.File.Exists(Path.Combine(GetPhotoDirectory(), user.Id + extension))),
            PhotoUrl = Url.Action(nameof(Photo))
        };

        await AddEmployeeDetailsAsync(model, user.Id);
        return model;
    }

    private static IEnumerable<string> GetPhotoExtensions() => [".jpg", ".png", ".webp"];

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
        model.BirthDate = employee.BirthDate;
        model.Address = employee.Address;
    }
}
