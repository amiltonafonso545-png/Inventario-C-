using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestorRHPro.Data;
using GestorRHPro.Models;
using GestorRHPro.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
namespace GestorRHPro.Controllers;
[Authorize(Roles = "Administrator,HRManager")]
public class EmployeesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public EmployeesController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }
    public async Task<IActionResult> Index(string? search, int? dept, EmployeeStatus? status)
    {
        var q = _db.Employees.Include(e=>e.Department).Include(e=>e.Position).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(e=>e.FullName.Contains(search)||e.Email.Contains(search));
        if (dept.HasValue) q = q.Where(e=>e.DepartmentId==dept);
        if (status.HasValue) q = q.Where(e=>e.Status==status);
        ViewBag.Departments = await _db.Departments.ToListAsync();
        return View(await q.ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Administrator")]
    public async Task<IActionResult> CreateAccount(int id)
    {
        Response.Headers.CacheControl = "no-store, no-cache";
        Response.Headers.Pragma = "no-cache";
        var result = new EmployeeAccountsResultViewModel();
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return NotFound();

        var email = employee.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(employee.UserId))
            result.Skipped.Add($"{employee.FullName}: já tem acesso associado.");
        else if (string.IsNullOrWhiteSpace(email))
            result.Skipped.Add($"{employee.FullName}: falta um email.");
        else if (await _db.Employees.AnyAsync(other => other.Id != employee.Id && other.Email == email))
            result.Skipped.Add($"{employee.FullName}: o email está repetido noutro registo de funcionário.");
        else if (await _userManager.FindByEmailAsync(email) is not null)
            result.Skipped.Add($"{employee.FullName}: já existe uma conta com este email; não foi associada automaticamente.");
        else
        {
            if (!await _roleManager.RoleExistsAsync("Employee"))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole("Employee"));
                if (!roleResult.Succeeded)
                    result.Skipped.Add("Não foi possível preparar a função de funcionário: "
                        + string.Join(" ", roleResult.Errors.Select(error => error.Description)));
            }

            if (result.Skipped.Count == 0)
            {
                var password = GenerateInitialPassword();
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = false,
                    FullName = employee.FullName,
                    Role = "Employee"
                };

                await using var transaction = await _db.Database.BeginTransactionAsync();
                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    result.Skipped.Add($"{employee.FullName}: {string.Join(" ", createResult.Errors.Select(error => error.Description))}");
                }
                else
                {
                    var addRoleResult = await _userManager.AddToRoleAsync(user, "Employee");
                    if (!addRoleResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        result.Skipped.Add($"{employee.FullName}: não foi possível atribuir a função de funcionário.");
                    }
                    else
                    {
                        employee.UserId = user.Id;
                        await _db.SaveChangesAsync();
                        await transaction.CommitAsync();
                        result.Created.Add(new EmployeeAccountCredential(employee.FullName, email, password));
                    }
                }
            }
        }

        return View("AccountsCreated", result);
    }
    private static string GenerateInitialPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%*?-_";
        var all = upper + lower + digits + symbols;
        var password = new char[24];
        password[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        password[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
        for (var index = 4; index < password.Length; index++)
            password[index] = all[RandomNumberGenerator.GetInt32(all.Length)];
        for (var index = password.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (password[index], password[swapIndex]) = (password[swapIndex], password[index]);
        }
        return new string(password);
    }
    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await _db.Departments.ToListAsync();
        ViewBag.Positions = await _db.Positions.ToListAsync();
        return View();
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Employee e)
    {
        if (!ModelState.IsValid) { ViewBag.Departments=await _db.Departments.ToListAsync(); ViewBag.Positions=await _db.Positions.ToListAsync(); return View(e); }
        _db.Employees.Add(e); await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id)
    {
        var e = await _db.Employees.FindAsync(id);
        if (e==null) return NotFound();
        ViewBag.Departments = await _db.Departments.ToListAsync();
        ViewBag.Positions = await _db.Positions.ToListAsync();
        return View(e);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Employee e)
    {
        if (id!=e.Id) return BadRequest();
        if (!ModelState.IsValid) { ViewBag.Departments=await _db.Departments.ToListAsync(); ViewBag.Positions=await _db.Positions.ToListAsync(); return View(e); }
        if (e.DepartmentId.HasValue && !await _db.Departments.AnyAsync(department => department.Id == e.DepartmentId.Value))
            ModelState.AddModelError(nameof(e.DepartmentId), "O departamento selecionado não existe.");
        if (e.PositionId.HasValue && !await _db.Positions.AnyAsync(position => position.Id == e.PositionId.Value))
            ModelState.AddModelError(nameof(e.PositionId), "O cargo selecionado não existe.");
        if (!ModelState.IsValid)
        {
            ViewBag.Departments=await _db.Departments.ToListAsync();
            ViewBag.Positions=await _db.Positions.ToListAsync();
            return View(e);
        }

        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return NotFound();
        employee.FullName = e.FullName.Trim();
        employee.Email = e.Email.Trim();
        employee.Phone = e.Phone?.Trim();
        employee.DepartmentId = e.DepartmentId;
        employee.PositionId = e.PositionId;
        employee.Status = e.Status;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Employees.Include(x=>x.Department).FirstOrDefaultAsync(x=>x.Id==id);
        if (e==null) return NotFound();
        ViewBag.HasPayrollHistory = await _db.PayrollRecords.AnyAsync(record => record.EmployeeId == id);
        return View(e);
    }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var e = await _db.Employees.FindAsync(id);
        if (e!=null)
        {
            if (await _db.PayrollRecords.AnyAsync(record => record.EmployeeId == id))
            {
                TempData["ErrorMessage"] = "Não é possível eliminar este funcionário porque tem histórico salarial. Altere o estado para inativo para preservar os registos.";
                return RedirectToAction(nameof(Delete), new { id });
            }
            _db.Employees.Remove(e);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Details(int id)
    {
        var e = await _db.Employees.Include(x=>x.Department).Include(x=>x.Position).FirstOrDefaultAsync(x=>x.Id==id);
        if (e==null) return NotFound();
        return View(e);
    }
}
