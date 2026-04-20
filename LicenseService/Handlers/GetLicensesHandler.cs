using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Models;
public class GetLicensesHandler : IRequestHandler<GetLicensesQuery, List<License>>
{
    private readonly LicenseDbContext _context;
    public GetLicensesHandler(LicenseDbContext context) => _context = context;
    public async Task<List<License>> Handle(GetLicensesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Licenses.AsQueryable(); // We use the global filter for TenantId automatically

        if (request.Role == "Applicant")
        {
            query = query.Where(l => l.UserId == request.UserId);
        }
        else if (request.Role == "Agency" && !string.IsNullOrEmpty(request.Agency))
        {
            query = query.Where(l => l.Agency == request.Agency);
        }

        return await query.ToListAsync(cancellationToken);
    }
}