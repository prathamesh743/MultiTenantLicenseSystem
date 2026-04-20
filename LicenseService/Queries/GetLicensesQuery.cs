using MediatR;
using SharedKernel.Models;
namespace LicenseService.Queries;

public record GetLicensesQuery(int? UserId = null, string? Role = null, string? Agency = null) : IRequest<List<License>>;