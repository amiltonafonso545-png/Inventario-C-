using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GestorRHPro.Data;
using GestorRHPro.Models;
namespace GestorRHPro.Controllers;
[Authorize(Roles = "Administrator,HRManager")]
public class DepartmentsController : Controller
{
    private readonly ApplicationDbContext _db;
    public DepartmentsController(ApplicationDbContext db) => _db = db;
    public async Task<IActionResult> Index() => View(await _db.Departments.Include(d=>d.Employees).ToListAsync());
    public IActionResult Create() => View();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Department d){ if(!ModelState.IsValid) return View(d); _db.Departments.Add(d); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Edit(int id){ var d=await _db.Departments.FindAsync(id); if(d==null) return NotFound(); return View(d); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Department d){ if(id!=d.Id) return BadRequest(); if(!ModelState.IsValid) return View(d); _db.Update(d); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Delete(int id){ var d=await _db.Departments.FindAsync(id); if(d==null) return NotFound(); return View(d); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id){ var d=await _db.Departments.FindAsync(id); if(d!=null){_db.Departments.Remove(d); await _db.SaveChangesAsync();} return RedirectToAction(nameof(Index)); }
}
