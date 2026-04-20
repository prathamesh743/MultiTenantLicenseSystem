using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Models;

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
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        => Ok(await _mediator.Send(new UpdateLicenseStatusCommand(id, status)));
}