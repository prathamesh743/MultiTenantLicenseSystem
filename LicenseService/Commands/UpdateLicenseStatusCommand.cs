using MediatR;
using SharedKernel.Models;

public record UpdateLicenseStatusCommand(int Id, string Status) : IRequest<bool>;
