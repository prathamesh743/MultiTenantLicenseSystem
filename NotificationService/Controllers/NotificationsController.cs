using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Models;
using System.Security.Claims;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly LicenseDbContext _context;

    public NotificationsController(LicenseDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserNotifications()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .ToListAsync();

        return Ok(notifications);
    }

    public record CreateNotificationDto(int UserId, string Message);

    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message)) return BadRequest("Message is required.");

        var callerUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(callerUserIdStr, out var callerUserId)) return Unauthorized();

        var isApplicant = User.IsInRole("Applicant");
        var targetUserId = isApplicant ? callerUserId : dto.UserId;

        if (!await _context.Users.AnyAsync(u => u.Id == targetUserId))
            return NotFound("Target user not found in this tenant.");

        var notification = new Notification
        {
            UserId = targetUserId,
            Message = dto.Message.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            TenantId = _context.TenantId
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return Ok(new { id = notification.Id });
    }

    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkRead()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in notifications) n.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok();
    }
}
