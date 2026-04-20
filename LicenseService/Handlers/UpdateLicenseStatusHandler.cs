using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Models;

public class UpdateLicenseStatusHandler : IRequestHandler<UpdateLicenseStatusCommand, bool>
{
    private readonly LicenseDbContext _context;

    public UpdateLicenseStatusHandler(LicenseDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateLicenseStatusCommand request, CancellationToken cancellationToken)
    {
        var license = await _context.Licenses.FindAsync(new object[] { request.Id }, cancellationToken);

        if (license == null) return false;

        // Business Logic: Once approved or rejected, status cannot be changed
        if (license.Status != "Pending") return false;

        license.Status = request.Status;
        
        // Add Notification for User
        var notification = new Notification
        {
            UserId = license.UserId,
            Message = $"Your license {license.LicenseNumber} has been {request.Status}.",
            TenantId = license.TenantId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
