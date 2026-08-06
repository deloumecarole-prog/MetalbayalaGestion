using System.ComponentModel.DataAnnotations;

namespace MetalBayalaGestion.Models;

public class Category : BaseEntity
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
