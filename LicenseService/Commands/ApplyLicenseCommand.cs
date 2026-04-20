using MediatR;
using SharedKernel.Models;
using System.Text.Json.Serialization;

namespace LicenseService.Commands;

public record ApplyLicenseCommand(
    [property: JsonPropertyName("ApplicantName")] string ApplicantName, 
    [property: JsonPropertyName("Agency")] string Agency, 
    [property: JsonPropertyName("UserId")] int UserId = 0, 
    [property: JsonPropertyName("DocumentId")] int? DocumentId = null,
    [property: JsonPropertyName("DocumentFileName")] string? DocumentFileName = null
) : IRequest<License>;