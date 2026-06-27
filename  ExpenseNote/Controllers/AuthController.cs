using ExpenseNote.DTOs;
using ExpenseNote.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseNote.Controllers;

[ApiController]
[Route("api/Auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Correo y contraseña son obligatorios." });

        var result = await _authService.RegisterAsync(dto);

        if (result == null)
            return BadRequest(new { message = "El correo ya está registrado." });

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (result == null)
            return Unauthorized(new { message = "Correo o contraseña incorrectos." });

        return Ok(result);
    }
}