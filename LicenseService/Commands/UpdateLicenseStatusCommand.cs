using MediatR;
using SharedKernel.Models;

namespace LicenseService.Commands;

public record UpdateLicenseStatusCommand(int Id, string Status, string? RequesterRole = null, string? RequesterAgency = null) : IRequest<bool>;
