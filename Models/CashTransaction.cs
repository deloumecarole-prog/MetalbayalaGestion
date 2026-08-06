using System.ComponentModel.DataAnnotations;

namespace MetalBayalaGestion.Models;

public class CashTransaction : BaseEntity
{
    public DateTime Date { get; set; } = DateTime.Now;
    public string Label { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Encaissement, Dépense, Entrée, Sortie
    public string Type { get; set; } = "Entrée"; // Entrée, Sortie
    public decimal Amount { get; set; }
    public string Mode { get; set; } = "Espèces";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public int? InvoiceId { get; set; }
    public virtual Invoice? Invoice { get; set; }
}
