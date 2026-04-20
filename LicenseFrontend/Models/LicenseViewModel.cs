using System.Text.Json.Serialization;

namespace LicenseFrontend.Models;

public class LicenseViewModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("documentId")]
    public int? DocumentId { get; set; }
    
    [JsonPropertyName("documentFileName")]
    public string? DocumentFileName { get; set; }
    
    [JsonPropertyName("licenseNumber")]
    public string LicenseNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("applicantName")]
    public string ApplicantName { get; set; } = string.Empty;
    
    [JsonPropertyName("issueDate")]
    public DateTime IssueDate { get; set; }
    
    [JsonPropertyName("expiryDate")]
    public DateTime ExpiryDate { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";
    
    [JsonPropertyName("agency")]
    public string Agency { get; set; } = string.Empty;
    
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;
}
