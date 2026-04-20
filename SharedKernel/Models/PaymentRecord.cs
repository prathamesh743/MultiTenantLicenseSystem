namespace SharedKernel.Models;

public class PaymentRecord : BaseEntity
{
    public int UserId { get; set; }
    public int LicenseId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Paid";
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
}
