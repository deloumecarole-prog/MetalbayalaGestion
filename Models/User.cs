using System.ComponentModel.DataAnnotations;

namespace MetalBayalaGestion.Models;

public class User : BaseEntity
{
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Administrateur";
    public bool IsActive { get; set; } = true;
    public string? FullName { get; set; }
}
