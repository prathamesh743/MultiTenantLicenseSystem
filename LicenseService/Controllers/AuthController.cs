using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Data;
using SharedKernel.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LicenseDbContext _context;

    public AuthController(LicenseDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == request.Username && u.TenantId == request.TenantId))
            return BadRequest("Username already exists for this tenant.");

        var user = new User
        {
            Username = request.Username,
            // In a real application, hash the password (e.g., BCrypt). Keeping it plain for simplicity in Phase 1 if needed, but let's do a simple base64 or keep it plain.
            PasswordHash = request.Password, 
            Role = request.Role,
            TenantId = request.TenantId,
            Agency = request.Agency
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Registration successful." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.PasswordHash == request.Password && u.TenantId == request.TenantId);

        if (user == null)
            return Unauthorized("Invalid credentials.");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("TenantId", user.TenantId)
        };

        if (!string.IsNullOrEmpty(user.Agency))
        {
            claims.Add(new Claim("Agency", user.Agency));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKeyForAssignment2026_ThisIsLongEnoughForHS256_2026"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "LicenseSystem",
            audience: "LicenseSystem",
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            role = user.Role,
            tenantId = user.TenantId
        });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class RegisterRequest : LoginRequest
{
    public string Role { get; set; } = "Applicant";
    public string? Agency { get; set; }
}