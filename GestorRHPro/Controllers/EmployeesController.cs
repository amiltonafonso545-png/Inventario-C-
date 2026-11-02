using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestorRHPro.Data;
using GestorRHPro.Models;
namespace GestorRHPro.Controllers;
[Authorize(Roles = "Administrator,HRManager")]
public class EmployeesController : Controller
{
    private readonly ApplicationDbContext _db;
    public EmployeesController(ApplicationDbContext db) => _db = db;
    public async Task<IActionResult> Index(string? search, int? dept, EmployeeStatus? status)
    {
        var q = _db.Employees.Include(e=>e.Department).Include(e=>e.Position).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(e=>e.FullName.Contains(search)||e.Email.Contains(search));
        if (dept.HasValue) q = q.Where(e=>e.DepartmentId==dept);
        if (status.HasValue) q = q.Where(e=>e.Status==status);
        ViewBag.Departments = await _db.Departments.ToListAsync();
        return View(await q.ToListAsync());
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
        _db.Update(e); await _db.SaveChangesAsync();
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
