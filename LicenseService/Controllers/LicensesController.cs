using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Models;
using LicenseService.Commands;
using LicenseService.Queries;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LicensesController : ControllerBase
{
    private readonly IMediator _mediator;
    public LicensesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Apply([FromBody] ApplyLicenseCommand cmd)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdStr, out var userId);
        
        var finalCmd = cmd with { UserId = userId };
        return Ok(await _mediator.Send(finalCmd));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdStr, out var userId);
        
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var agency = User.FindFirst("Agency")?.Value;

        return Ok(await _mediator.Send(new GetLicensesQuery(userId, role, agency)));
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Agency,Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateLicenseStatusRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Status))
            return BadRequest("Status is required.");

        var status = request.Status.Trim();
        if (status is not ("Approved" or "Rejected"))
            return BadRequest("Status must be Approved or Rejected.");

        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var agency = User.FindFirst("Agency")?.Value;
        return Ok(await _mediator.Send(new UpdateLicenseStatusCommand(id, status, role, agency)));
    }

    public sealed record UpdateLicenseStatusRequest(string Status);
}
