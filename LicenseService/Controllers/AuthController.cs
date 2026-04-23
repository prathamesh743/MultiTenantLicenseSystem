using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Data;
using SharedKernel.Models;
using SharedKernel.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LicenseDbContext _context;
    private readonly JwtOptions _jwt;

    public AuthController(LicenseDbContext context, IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _jwt = jwtOptions.Value;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.TenantId))
            return BadRequest("Username, password, and tenantId are required.");

        if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == request.Username && u.TenantId == request.TenantId))
            return BadRequest("Username already exists for this tenant.");

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Applicant",
            TenantId = request.TenantId,
            Agency = null
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Registration successful.", role = user.Role });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.TenantId))
            return BadRequest("Username, password, and tenantId are required.");

        var user = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.TenantId == request.TenantId);

        if (user == null)
            return Unauthorized("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
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

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
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

    public record CreateUserRequest(string Username, string Password, string TenantId, string Role, string? Agency);

    [Authorize(Roles = "Admin")]
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.TenantId))
            return BadRequest("Username, password, and tenantId are required.");

        var role = request.Role?.Trim();
        if (role is not ("Applicant" or "Agency" or "Admin"))
            return BadRequest("Role must be Applicant, Agency, or Admin.");

        if (role == "Agency" && string.IsNullOrWhiteSpace(request.Agency))
            return BadRequest("Agency is required for Agency role.");

        if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == request.Username && u.TenantId == request.TenantId))
            return BadRequest("Username already exists for this tenant.");

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            TenantId = request.TenantId,
            Agency = role == "Agency" ? request.Agency : null
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { id = user.Id, role = user.Role, tenantId = user.TenantId });
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
    // Role/Agency selection is handled by admins only via /api/Auth/users.
}
