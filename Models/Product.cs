using System.ComponentModel.DataAnnotations;

namespace MetalBayalaGestion.Models;

public class Product : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    [Required]
    public string Designation { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public string Unit { get; set; } = "U";
    public decimal StockQuantity { get; set; }
    public decimal MinStock { get; set; }
    public int? SupplierId { get; set; }
    public virtual Supplier? Supplier { get; set; }
    public bool IsActive { get; set; } = true;
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public virtual ICollection<QuoteLine> QuoteLines { get; set; } = new List<QuoteLine>();
    public virtual ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
}
