using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestorRHPro.Data;
using GestorRHPro.Models;
using GestorRHPro.ViewModels;

namespace GestorRHPro.Controllers;

[Authorize(Roles = "Administrator,HRManager")]
public sealed class PayrollController : Controller
{
    private readonly ApplicationDbContext _db;

    public PayrollController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var records = await _db.PayrollRecords
            .Include(record => record.Employee)
            .OrderByDescending(record => record.Year)
            .ThenByDescending(record => record.Month)
            .ThenBy(record => record.Employee.FullName)
            .ToListAsync();
        return View(records);
    }

    public async Task<IActionResult> Create()
    {
        await LoadEmployeesAsync();
        return View(new PayrollFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PayrollFormViewModel input)
    {
        if (input.EmployeeId < 1)
            ModelState.AddModelError(nameof(input.EmployeeId), "Selecione um funcionário.");
        else if (await _db.PayrollRecords.AnyAsync(record => record.EmployeeId == input.EmployeeId && record.Year == input.Year && record.Month == input.Month))
            ModelState.AddModelError(string.Empty, "Já existe um registo salarial para este funcionário e período.");

        if (!ModelState.IsValid)
        {
            await LoadEmployeesAsync();
            return View(input);
        }

        _db.PayrollRecords.Add(new PayrollRecord
        {
            EmployeeId = input.EmployeeId,
            Year = input.Year,
            Month = input.Month,
            BaseSalary = input.BaseSalary,
            Bonuses = input.Bonuses,
            Deductions = input.Deductions,
            Note = input.Note?.Trim()
        });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var record = await _db.PayrollRecords.FindAsync(id);
        if (record is null) return NotFound();

        await LoadEmployeesAsync();
        return View(new PayrollFormViewModel
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            Year = record.Year,
            Month = record.Month,
            BaseSalary = record.BaseSalary,
            Bonuses = record.Bonuses,
            Deductions = record.Deductions,
            Note = record.Note
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PayrollFormViewModel input)
    {
        if (id != input.Id) return BadRequest();
        var record = await _db.PayrollRecords.FindAsync(id);
        if (record is null) return NotFound();

        if (input.EmployeeId < 1)
            ModelState.AddModelError(nameof(input.EmployeeId), "Selecione um funcionário.");
        else if (await _db.PayrollRecords.AnyAsync(other => other.Id != id && other.EmployeeId == input.EmployeeId && other.Year == input.Year && other.Month == input.Month))
            ModelState.AddModelError(string.Empty, "Já existe um registo salarial para este funcionário e período.");

        if (!ModelState.IsValid)
        {
            await LoadEmployeesAsync();
            return View(input);
        }

        record.EmployeeId = input.EmployeeId;
        record.Year = input.Year;
        record.Month = input.Month;
        record.BaseSalary = input.BaseSalary;
        record.Bonuses = input.Bonuses;
        record.Deductions = input.Deductions;
        record.Note = input.Note?.Trim();
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _db.PayrollRecords.FindAsync(id);
        if (record is not null)
        {
            _db.PayrollRecords.Remove(record);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadEmployeesAsync()
    {
        var employees = await _db.Employees
            .OrderBy(employee => employee.FullName)
            .Select(employee => new { employee.Id, employee.FullName, employee.InternalNumber })
            .ToListAsync();
        ViewBag.Employees = new SelectList(employees, "Id", "FullName");
    }
}
