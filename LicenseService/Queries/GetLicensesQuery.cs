using MediatR;
using SharedKernel.Models;
public record GetLicensesQuery(int? UserId = null, string? Role = null, string? Agency = null) : IRequest<List<License>>;