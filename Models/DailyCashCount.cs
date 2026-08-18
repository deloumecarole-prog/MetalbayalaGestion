using System;

namespace MetalBayalaGestion.Models;

// Comptage physique de caisse saisi manuellement, un par jour, pour calculer
// l'ecart entre la caisse theorique (calculee a partir des transactions) et
// la caisse reellement comptee en fin de journee.
public class DailyCashCount : BaseEntity
{
    public DateTime Date { get; set; } = DateTime.Now.Date;
    public decimal CountedAmount { get; set; }
    public string? Notes { get; set; }
}
