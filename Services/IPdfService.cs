using MetalBayalaGestion.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetalBayalaGestion.Services;

public interface IPdfService
{
    Task GenerateQuotePdfAsync(Quote quote, string filePath);
    Task GenerateInvoicePdfAsync(Invoice invoice, string filePath);
    Task GenerateDeliveryNotePdfAsync(DeliveryNote note, string filePath);
    Task GenerateReportPdfAsync(DateTime startDate, DateTime endDate, decimal totalSales, decimal totalCashIn,
        decimal totalReceivables, decimal totalExpenses, decimal cashBalance,
        List<StockMovement> stockMovements, List<Product> lowStockProducts, string filePath);

    // Envoie un PDF deja genere directement vers l'imprimante par defaut de Windows
    Task PrintFileAsync(string filePath);
}
