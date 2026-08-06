using System.ComponentModel.DataAnnotations;

namespace MetalBayalaGestion.Models;

public class Invoice : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public DateTime DueDate { get; set; } = DateTime.Now.AddDays(30);
    public int ClientId { get; set; }
    public virtual Client Client { get; set; } = null!;
    public string? ClientAddress { get; set; }
    public string? ClientPhone { get; set; }
    public string Status { get; set; } = "Brouillon"; // Brouillon, Impayée, Partiellement payée, Payée, Annulée
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance => Total - PaidAmount;
    public int? QuoteId { get; set; }
    public virtual Quote? Quote { get; set; }
    public int? OrderId { get; set; }
    public virtual Order? Order { get; set; }
    public string? Notes { get; set; }
    public virtual ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
