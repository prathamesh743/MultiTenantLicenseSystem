namespace SharedKernel.Models;
public class License : BaseEntity
{
    public int UserId { get; set; }
    public int? DocumentId { get; set; }
    public string? DocumentFileName { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = "Pending";
    public string Agency { get; set; } = string.Empty;
}