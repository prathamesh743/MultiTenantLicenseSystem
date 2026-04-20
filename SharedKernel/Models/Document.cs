namespace SharedKernel.Models;
public class Document : BaseEntity
{
    public int LicenseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}