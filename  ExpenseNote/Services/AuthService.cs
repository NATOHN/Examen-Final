using ExpenseNote.DTOs;
using Google.Cloud.Firestore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ExpenseNote.Services;

public class AuthService
{
    private readonly FirestoreDb _db;
    private readonly IConfiguration _config;
    private const string CollectionName = "users";

    public AuthService(FirestoreDb db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        var existing = await _db.Collection(CollectionName)
            .WhereEqualTo("Email", dto.Email)
            .Limit(1)
            .GetSnapshotAsync();

        if (existing.Count > 0)
            return null; // correo ya registrado

        var userId = Guid.NewGuid().ToString();
        var passwordHash = HashPassword(dto.Password);

        var data = new Dictionary<string, object>
        {
            { "FullName", dto.FullName },
            { "Email", dto.Email },
            { "PasswordHash", passwordHash },
            { "CreatedAt", Timestamp.GetCurrentTimestamp() }
        };

        await _db.Collection(CollectionName).Document(userId).SetAsync(data);

        var token = GenerateJwtToken(userId, dto.Email);

        return new AuthResponseDto
        {
            Token = token,
            FullName = dto.FullName,
            Email = dto.Email
        };
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var snapshot = await _db.Collection(CollectionName)
            .WhereEqualTo("Email", dto.Email)
            .Limit(1)
            .GetSnapshotAsync();

        if (snapshot.Count == 0)
            return null;

        var doc = snapshot.Documents[0];
        var storedHash = doc.GetValue<string>("PasswordHash");

        if (!VerifyPassword(dto.Password, storedHash))
            return null;

        var fullName = doc.GetValue<string>("FullName");
        var token = GenerateJwtToken(doc.Id, dto.Email);

        return new AuthResponseDto
        {
            Token = token,
            FullName = fullName,
            Email = dto.Email
        };
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == storedHash;
    }

    private string GenerateJwtToken(string userId, string email)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}