using System.ComponentModel.DataAnnotations;

namespace MetalBayalaGestion.Models;

public class Supplier : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Nif { get; set; }
    public string? Rccm { get; set; }
    public string? Notes { get; set; }
}
