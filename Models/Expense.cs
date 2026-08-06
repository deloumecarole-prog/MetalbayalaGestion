using System.ComponentModel.DataAnnotations;

namespace MetalBayalaGestion.Models;

public class Expense : BaseEntity
{
    public string Category { get; set; } = "Autre";
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string Mode { get; set; } = "Espèces";
    public int? SupplierId { get; set; }
    public virtual Supplier? Supplier { get; set; }
    public string? Notes { get; set; }
}
