using System.Security.Claims;
using ExpenseNote.DTOs;
using ExpenseNote.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseNote.Controllers;

[ApiController]
[Route("api/Expense")]
[Authorize]
public class ExpenseController : ControllerBase
{
    private readonly ExpenseService _expenseService;

    public ExpenseController(ExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseDto dto)
    {
        var userId = GetUserId();
        var result = await _expenseService.CreateExpenseAsync(dto, userId);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var result = await _expenseService.GetExpensesByUserIdAsync(userId);
        return Ok(result);
    }
}