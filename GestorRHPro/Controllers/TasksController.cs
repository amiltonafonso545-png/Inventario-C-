using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestorRHPro.Data;
using GestorRHPro.Models;
using GestorRHPro.ViewModels;

namespace GestorRHPro.Controllers;

[Authorize(Roles = "Administrator,HRManager")]
public sealed class TasksController : Controller
{
    private readonly ApplicationDbContext _db;

    public TasksController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var tasks = await _db.TaskItems
            .Include(task => task.AssignedEmployee)
            .OrderBy(task => task.Status == WorkItemStatus.Done)
            .ThenBy(task => task.DueDate)
            .ThenByDescending(task => task.Priority)
            .ToListAsync();
        return View(tasks);
    }

    public async Task<IActionResult> Create()
    {
        await LoadEmployeesAsync();
        return View(new TaskFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskFormViewModel input)
    {
        if (!Enum.IsDefined(input.Priority) || !Enum.IsDefined(input.Status))
            ModelState.AddModelError(string.Empty, "A prioridade ou o estado selecionado não é válido.");
        if (input.AssignedEmployeeId.HasValue && !await _db.Employees.AnyAsync(employee => employee.Id == input.AssignedEmployeeId.Value))
            ModelState.AddModelError(nameof(input.AssignedEmployeeId), "O funcionário selecionado não existe.");

        if (!ModelState.IsValid)
        {
            await LoadEmployeesAsync();
            return View(input);
        }

        var task = new TaskItem
        {
            Title = input.Title.Trim(),
            Description = input.Description?.Trim(),
            DueDate = input.DueDate?.Date,
            Priority = input.Priority,
            Status = input.Status,
            AssignedEmployeeId = input.AssignedEmployeeId,
            CompletedAt = input.Status == WorkItemStatus.Done ? DateTime.UtcNow : null
        };
        _db.TaskItems.Add(task);

        var assignedEmployee = input.AssignedEmployeeId.HasValue
            ? await _db.Employees.FindAsync(input.AssignedEmployeeId.Value)
            : null;
        if (!string.IsNullOrWhiteSpace(assignedEmployee?.UserId))
        {
            _db.Notifications.Add(new Notification
            {
                UserId = assignedEmployee.UserId,
                Title = "Nova tarefa atribuída",
                Message = $"A tarefa «{task.Title}» foi atribuída a si.",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
        TempData["TaskNotice"] = assignedEmployee is null
            ? "Tarefa criada sem funcionário atribuído."
            : string.IsNullOrWhiteSpace(assignedEmployee.UserId)
                ? "Tarefa criada, mas o funcionário ainda não tem uma conta para receber notificações."
                : "Tarefa criada e funcionário notificado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var task = await _db.TaskItems.FindAsync(id);
        if (task is null) return NotFound();

        await LoadEmployeesAsync();
        return View(new TaskFormViewModel
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            Priority = task.Priority,
            Status = task.Status,
            AssignedEmployeeId = task.AssignedEmployeeId
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaskFormViewModel input)
    {
        if (id != input.Id) return BadRequest();
        var task = await _db.TaskItems.FindAsync(id);
        if (task is null) return NotFound();

        if (!Enum.IsDefined(input.Priority) || !Enum.IsDefined(input.Status))
            ModelState.AddModelError(string.Empty, "A prioridade ou o estado selecionado não é válido.");
        if (input.AssignedEmployeeId.HasValue && !await _db.Employees.AnyAsync(employee => employee.Id == input.AssignedEmployeeId.Value))
            ModelState.AddModelError(nameof(input.AssignedEmployeeId), "O funcionário selecionado não existe.");

        if (!ModelState.IsValid)
        {
            await LoadEmployeesAsync();
            return View(input);
        }

        var previousAssignedEmployeeId = task.AssignedEmployeeId;
        task.Title = input.Title.Trim();
        task.Description = input.Description?.Trim();
        task.DueDate = input.DueDate?.Date;
        task.Priority = input.Priority;
        task.AssignedEmployeeId = input.AssignedEmployeeId;
        if (task.Status != input.Status)
        {
            task.Status = input.Status;
            task.CompletedAt = input.Status == WorkItemStatus.Done ? DateTime.UtcNow : null;
        }
        if (previousAssignedEmployeeId != input.AssignedEmployeeId)
        {
            if (previousAssignedEmployeeId.HasValue)
            {
                var previousEmployee = await _db.Employees.FindAsync(previousAssignedEmployeeId.Value);
                if (!string.IsNullOrWhiteSpace(previousEmployee?.UserId))
                {
                    _db.Notifications.Add(new Notification
                    {
                        UserId = previousEmployee.UserId,
                        Title = "Tarefa atualizada",
                        Message = $"A tarefa «{task.Title}» deixou de estar atribuída a si.",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (input.AssignedEmployeeId.HasValue)
            {
                var newEmployee = await _db.Employees.FindAsync(input.AssignedEmployeeId.Value);
                if (!string.IsNullOrWhiteSpace(newEmployee?.UserId))
                {
                    _db.Notifications.Add(new Notification
                    {
                        UserId = newEmployee.UserId,
                        Title = "Nova tarefa atribuída",
                        Message = $"A tarefa «{task.Title}» foi atribuída a si.",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }
        await _db.SaveChangesAsync();
        TempData["TaskNotice"] = "Tarefa atualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, WorkItemStatus status)
    {
        if (!Enum.IsDefined(status)) return BadRequest();
        var task = await _db.TaskItems.FindAsync(id);
        if (task is null) return NotFound();

        task.Status = status;
        task.CompletedAt = status == WorkItemStatus.Done ? DateTime.UtcNow : null;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _db.TaskItems.FindAsync(id);
        if (task is not null)
        {
            _db.TaskItems.Remove(task);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadEmployeesAsync()
    {
        var employees = await _db.Employees
            .Where(employee => employee.Status == EmployeeStatus.Active)
            .OrderBy(employee => employee.FullName)
            .Select(employee => new { employee.Id, employee.FullName })
            .ToListAsync();
        ViewBag.Employees = new SelectList(employees, "Id", "FullName");
    }
}
