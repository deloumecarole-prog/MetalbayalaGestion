using System.ComponentModel.DataAnnotations;

namespace MetalBayalaGestion.Models;

public class Quote : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public DateTime ValidUntil { get; set; } = DateTime.Now.AddDays(30);
    public int ClientId { get; set; }
    public virtual Client Client { get; set; } = null!;
    public string? ClientAddress { get; set; }
    public string? ClientPhone { get; set; }
    public string Status { get; set; } = "Brouillon"; // Brouillon, Envoyé, Accepté, Refusé, Expiré, Converti
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
    public string? ConvertedTo { get; set; }
    public virtual ICollection<QuoteLine> Lines { get; set; } = new List<QuoteLine>();
}
