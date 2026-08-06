using System.ComponentModel.DataAnnotations;

namespace MetalBayalaGestion.Models;

public class Company : BaseEntity
{
    public string Name { get; set; } = "Metal Bayala";
    public string? LogoPath { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Nif { get; set; }
    public string? Rccm { get; set; }
    public string? TaxInfo { get; set; }
    public string? DocumentFooter { get; set; } = "Merci de votre confiance. Metal Bayala - Mali";
    public string Currency { get; set; } = "FCFA";
    public string CurrencyCode { get; set; } = "XOF";
}
