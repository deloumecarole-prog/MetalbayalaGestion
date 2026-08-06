using System.ComponentModel.DataAnnotations;

namespace MetalBayalaGestion.Models;

public class Order : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public int ClientId { get; set; }
    public virtual Client Client { get; set; } = null!;
    public string Status { get; set; } = "En attente";
    public int? QuoteId { get; set; }
    public virtual Quote? Quote { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public virtual ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
}
