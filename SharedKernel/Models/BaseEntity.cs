using System.ComponentModel.DataAnnotations;
namespace SharedKernel.Models;
public abstract class BaseEntity
{
    [Key] public int Id { get; set; }
    [Required] public string TenantId { get; set; } = string.Empty;
}