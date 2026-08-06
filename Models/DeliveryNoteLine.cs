namespace MetalBayalaGestion.Models;

public class DeliveryNoteLine : BaseEntity
{
    public int DeliveryNoteId { get; set; }
    public virtual DeliveryNote DeliveryNote { get; set; } = null!;
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    public string Designation { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "U";
}
