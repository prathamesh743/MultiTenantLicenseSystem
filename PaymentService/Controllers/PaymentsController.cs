using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Models;
using System.Security.Claims;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly LicenseDbContext _context;

    public PaymentsController(LicenseDbContext context)
    {
        _context = context;
    }

    [HttpPost("pay")]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        if (request.Amount <= 0) return BadRequest("Amount must be positive.");

        var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == request.LicenseId);
        if (license == null) return NotFound("License not found.");
        if (license.UserId != userId) return Forbid();

        // MOCK PAYMENT LOGIC
        var payment = new PaymentRecord
        {
            UserId = userId,
            LicenseId = request.LicenseId,
            Amount = request.Amount,
            Status = "Paid",
            PaymentDate = DateTime.UtcNow,
            TenantId = _context.TenantId
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Payment successful. License processing started." });
    }
}

public class PaymentRequest
{
    public int LicenseId { get; set; }
    public decimal Amount { get; set; }
}
