using System.Text.Json.Serialization;

namespace LicenseFrontend.Models;

public class NotificationViewModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }
}
