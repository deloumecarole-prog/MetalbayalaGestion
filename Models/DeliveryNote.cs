using System.ComponentModel.DataAnnotations;

namespace MetalBayalaGestion.Models;

public class DeliveryNote : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public int ClientId { get; set; }
    public virtual Client Client { get; set; } = null!;
    public string? DeliveryAddress { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = "Préparé";
    public int? OrderId { get; set; }
    public virtual Order? Order { get; set; }
    public string? Notes { get; set; }
    public virtual ICollection<DeliveryNoteLine> Lines { get; set; } = new List<DeliveryNoteLine>();
}
