using MediatR;
using SharedKernel.Data;
using SharedKernel.Models;
/// <summary>
/// Command Handler for creating a new license application.
/// Implements the Command side of the CQRS pattern.
/// </summary>
public class ApplyLicenseHandler : IRequestHandler<ApplyLicenseCommand, License>
{
    private readonly LicenseDbContext _context;
    public ApplyLicenseHandler(LicenseDbContext context) => _context = context;

    public async Task<License> Handle(ApplyLicenseCommand request, CancellationToken cancellationToken)
    {
        // Create new license entity. The TenantId is automatically populated from the context
        // to ensure the data belongs to the correct agency/tenant.
        var license = new License { 
            UserId = request.UserId,
            DocumentId = request.DocumentId,
            DocumentFileName = request.DocumentFileName,
            LicenseNumber = $"LIC-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            ApplicantName = request.ApplicantName, 
            Agency = request.Agency, 
            TenantId = _context.TenantId, 
            IssueDate = DateTime.UtcNow, 
            ExpiryDate = DateTime.UtcNow.AddYears(1) 
        };
        _context.Licenses.Add(license);
        await _context.SaveChangesAsync(cancellationToken);
        return license;
    }
}