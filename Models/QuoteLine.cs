namespace MetalBayalaGestion.Models;

public class QuoteLine : BaseEntity
{
    public int QuoteId { get; set; }
    public virtual Quote Quote { get; set; } = null!;
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    public string? ProductReference { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "U";
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Total => Quantity * UnitPrice - Discount;
}
