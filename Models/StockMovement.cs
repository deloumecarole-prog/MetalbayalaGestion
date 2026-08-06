using System.ComponentModel.DataAnnotations;

namespace MetalBayalaGestion.Models;

public class StockMovement : BaseEntity
{
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    public string Type { get; set; } = "Entrée"; // Entrée, Sortie, Ajustement
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
