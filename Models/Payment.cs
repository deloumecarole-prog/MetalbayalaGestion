using System.ComponentModel.DataAnnotations;

namespace MetalBayalaGestion.Models;

public class Payment : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public int ClientId { get; set; }
    public virtual Client Client { get; set; } = null!;
    public int? InvoiceId { get; set; }
    public virtual Invoice? Invoice { get; set; }
    public decimal Amount { get; set; }
    public string Mode { get; set; } = "Espèces"; // Espèces, Orange Money, Moov Money, Wave, Virement, Chèque, Autre
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
