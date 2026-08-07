using MetalBayalaGestion.Data;
using MetalBayalaGestion.Models;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MetalBayalaGestion.Services;

public class PdfService : IPdfService
{
    private readonly AppDbContext _context;

    static PdfService()
    {
        // PDFsharp 6.x ne detecte plus automatiquement les polices Windows :
        // il faut un IFontResolver explicite (voir WindowsFontResolver.cs).
        GlobalFontSettings.FontResolver ??= new WindowsFontResolver();
    }

    public PdfService(AppDbContext context)
    {
        _context = context;
    }

    // Envoie un PDF deja genere directement vers l'imprimante par defaut de Windows,
    // via le verbe "print" du shell (utilise le lecteur PDF installe : Edge, Adobe, etc.)
    public Task PrintFileAsync(string filePath)
    {
        return Task.Run(() =>
        {
            var psi = new ProcessStartInfo(filePath)
            {
                UseShellExecute = true,
                Verb = "print"
            };
            using var process = Process.Start(psi);
        });
    }

    public async Task GenerateQuotePdfAsync(Quote quote, string filePath)
    {
        await Task.Run(() =>
        {
            var company = _context.Companies.FirstOrDefault() ?? new Company();
            var lines = _context.QuoteLines.Where(l => l.QuoteId == quote.Id).ToList();
            GenerateDocumentPdf(filePath, "DEVIS", quote.Number, quote.Date, quote.ValidUntil, quote.Client, quote.ClientAddress, quote.ClientPhone,
                lines.Select(l => new DocLine { Ref = l.ProductReference ?? "", Designation = l.Designation, Qty = l.Quantity, Unit = l.Unit, Price = l.UnitPrice, Discount = l.Discount, Total = l.Total }).ToList(),
                quote.SubTotal, quote.Discount, quote.TaxRate, quote.TaxAmount, quote.Total, company, quote.PaymentTerms, quote.Notes, false);
        });
    }

    public async Task GenerateInvoicePdfAsync(Invoice invoice, string filePath)
    {
        await Task.Run(() =>
        {
            var company = _context.Companies.FirstOrDefault() ?? new Company();
            var lines = _context.InvoiceLines.Where(l => l.InvoiceId == invoice.Id).ToList();
            GenerateDocumentPdf(filePath, "FACTURE", invoice.Number, invoice.Date, invoice.DueDate, invoice.Client, invoice.ClientAddress, invoice.ClientPhone,
                lines.Select(l => new DocLine { Ref = l.ProductReference ?? "", Designation = l.Designation, Qty = l.Quantity, Unit = l.Unit, Price = l.UnitPrice, Discount = l.Discount, Total = l.Total }).ToList(),
                invoice.SubTotal, invoice.Discount, invoice.TaxRate, invoice.TaxAmount, invoice.Total, company, null, invoice.Notes, true, invoice.PaidAmount, invoice.Balance);
        });
    }

    public async Task GenerateDeliveryNotePdfAsync(DeliveryNote note, string filePath)
    {
        await Task.Run(() =>
        {
            var company = _context.Companies.FirstOrDefault() ?? new Company();
            var lines = _context.DeliveryNoteLines.Where(l => l.DeliveryNoteId == note.Id).ToList();
            GenerateDocumentPdf(filePath, "BON DE LIVRAISON", note.Number, note.Date, null, note.Client, note.DeliveryAddress, note.Phone,
                lines.Select(l => new DocLine { Ref = "", Designation = l.Designation, Qty = l.Quantity, Unit = l.Unit, Price = 0, Discount = 0, Total = 0 }).ToList(),
                0, 0, 0, 0, 0, company, null, note.Notes, false);
        });
    }

    public async Task GenerateReportPdfAsync(DateTime startDate, DateTime endDate, decimal totalSales, decimal totalCashIn,
        decimal totalReceivables, decimal totalExpenses, decimal cashBalance,
        List<StockMovement> stockMovements, List<Product> lowStockProducts, string filePath)
    {
        await Task.Run(() =>
        {
            var company = _context.Companies.FirstOrDefault() ?? new Company();
            var document = new PdfDocument();
            document.Info.Title = $"Rapport {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
            document.Info.Author = company.Name;

            var page = document.AddPage();
            page.Width = XUnit.FromMillimeter(210); // A4
            page.Height = XUnit.FromMillimeter(297);
            var gfx = XGraphics.FromPdfPage(page);
            var fontRegular = new XFont("Arial", 9, XFontStyleEx.Regular);
            var fontBold = new XFont("Arial", 9, XFontStyleEx.Bold);
            var fontTitle = new XFont("Arial", 16, XFontStyleEx.Bold);
            var fontHeader = new XFont("Arial", 11, XFontStyleEx.Bold);

            double margin = 25;
            double y = margin;
            double pageWidth = page.Width.Point;
            double contentWidth = pageWidth - 2 * margin;

            gfx.DrawString(company.Name.ToUpper(), fontHeader, XBrushes.Black, new XRect(margin, y, contentWidth, 20), XStringFormats.TopLeft);
            y += 15;
            gfx.DrawString($"RAPPORT D'ACTIVITÉ", fontTitle, XBrushes.Black, new XRect(margin, y, contentWidth, 25), XStringFormats.TopCenter);
            y += 20;
            gfx.DrawString($"Période : {startDate:dd/MM/yyyy} au {endDate:dd/MM/yyyy}", fontRegular, XBrushes.Black, new XRect(margin, y, contentWidth, 15), XStringFormats.TopCenter);
            y += 25;

            double col1 = margin;
            double col2 = margin + contentWidth / 2;
            gfx.DrawString($"Total ventes : {totalSales:N0} FCFA", fontBold, XBrushes.Black, new XRect(col1, y, contentWidth / 2, 15), XStringFormats.TopLeft);
            gfx.DrawString($"Total encaissé : {totalCashIn:N0} FCFA", fontBold, XBrushes.Black, new XRect(col2, y, contentWidth / 2, 15), XStringFormats.TopLeft);
            y += 15;
            gfx.DrawString($"Créances : {totalReceivables:N0} FCFA", fontBold, XBrushes.DarkRed, new XRect(col1, y, contentWidth / 2, 15), XStringFormats.TopLeft);
            gfx.DrawString($"Dépenses : {totalExpenses:N0} FCFA", fontBold, XBrushes.Black, new XRect(col2, y, contentWidth / 2, 15), XStringFormats.TopLeft);
            y += 15;
            gfx.DrawString($"Solde caisse : {cashBalance:N0} FCFA", fontBold, XBrushes.DarkGreen, new XRect(col1, y, contentWidth / 2, 15), XStringFormats.TopLeft);
            y += 25;

            gfx.DrawString("MOUVEMENTS DE STOCK", fontBold, XBrushes.Black, new XRect(margin, y, contentWidth, 15), XStringFormats.TopLeft);
            y += 15;
            gfx.DrawLine(XPens.Black, margin, y, pageWidth - margin, y);
            y += 5;
            gfx.DrawString("Date", fontBold, XBrushes.Black, new XRect(margin, y, 80, 15), XStringFormats.TopLeft);
            gfx.DrawString("Produit", fontBold, XBrushes.Black, new XRect(margin + 80, y, 180, 15), XStringFormats.TopLeft);
            gfx.DrawString("Type", fontBold, XBrushes.Black, new XRect(margin + 260, y, 60, 15), XStringFormats.TopLeft);
            gfx.DrawString("Qté", fontBold, XBrushes.Black, new XRect(margin + 320, y, 60, 15), XStringFormats.TopRight);
            gfx.DrawString("Référence", fontBold, XBrushes.Black, new XRect(margin + 380, y, 120, 15), XStringFormats.TopLeft);
            y += 12;
            gfx.DrawLine(XPens.Gray, margin, y, pageWidth - margin, y);
            y += 5;

            foreach (var m in stockMovements)
            {
                gfx.DrawString(m.CreatedAt.ToString("dd/MM/yy HH:mm"), fontRegular, XBrushes.Black, new XRect(margin, y, 80, 15), XStringFormats.TopLeft);
                gfx.DrawString(m.Product?.Designation ?? "", fontRegular, XBrushes.Black, new XRect(margin + 80, y, 180, 15), XStringFormats.TopLeft);
                gfx.DrawString(m.Type, fontRegular, XBrushes.Black, new XRect(margin + 260, y, 60, 15), XStringFormats.TopLeft);
                gfx.DrawString(m.Quantity.ToString("0.##"), fontRegular, XBrushes.Black, new XRect(margin + 320, y, 60, 15), XStringFormats.TopRight);
                gfx.DrawString(m.Reference ?? "", fontRegular, XBrushes.Black, new XRect(margin + 380, y, 120, 15), XStringFormats.TopLeft);
                y += 12;
            }

            y += 15;
            gfx.DrawString("PRODUITS EN STOCK BAS", fontBold, XBrushes.Black, new XRect(margin, y, contentWidth, 15), XStringFormats.TopLeft);
            y += 15;
            gfx.DrawLine(XPens.Black, margin, y, pageWidth - margin, y);
            y += 5;
            gfx.DrawString("Référence", fontBold, XBrushes.Black, new XRect(margin, y, 100, 15), XStringFormats.TopLeft);
            gfx.DrawString("Désignation", fontBold, XBrushes.Black, new XRect(margin + 100, y, 250, 15), XStringFormats.TopLeft);
            gfx.DrawString("Stock", fontBold, XBrushes.Black, new XRect(margin + 350, y, 60, 15), XStringFormats.TopRight);
            gfx.DrawString("Min", fontBold, XBrushes.Black, new XRect(margin + 410, y, 60, 15), XStringFormats.TopRight);
            y += 12;
            gfx.DrawLine(XPens.Gray, margin, y, pageWidth - margin, y);
            y += 5;

            foreach (var p in lowStockProducts)
            {
                gfx.DrawString(p.Reference, fontRegular, XBrushes.Black, new XRect(margin, y, 100, 15), XStringFormats.TopLeft);
                gfx.DrawString(p.Designation, fontRegular, XBrushes.Black, new XRect(margin + 100, y, 250, 15), XStringFormats.TopLeft);
                gfx.DrawString(p.StockQuantity.ToString("0.##"), fontRegular, XBrushes.DarkRed, new XRect(margin + 350, y, 60, 15), XStringFormats.TopRight);
                gfx.DrawString(p.MinStock.ToString("0.##"), fontRegular, XBrushes.Black, new XRect(margin + 410, y, 60, 15), XStringFormats.TopRight);
                y += 12;
            }

            document.Save(filePath);
        });
    }

    private class DocLine
    {
        public string Ref { get; set; } = "";
        public string Designation { get; set; } = "";
        public decimal Qty { get; set; }
        public string Unit { get; set; } = "";
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
    }

    // Genere un document A5 (devis / facture / bon de livraison) dont la mise en page
    // reprend le modele papier existant de l'entreprise : bandeau societe, ligne "DOIT:",
    // badge type de document, tableau N/Date/Validite, tableau des lignes, cases de
    // totaux encadrees, et zone de signature "Pour Acquit" / "Le Fournisseur".
    private void GenerateDocumentPdf(string filePath, string docType, string number, DateTime date, DateTime? validUntil,
        Client client, string? address, string? phone, List<DocLine> lines, decimal subTotal, decimal discount, decimal taxRate, decimal taxAmount, decimal total,
        Company company, string? paymentTerms, string? notes, bool showPayment, decimal paid = 0, decimal balance = 0)
    {
        var document = new PdfDocument();
        document.Info.Title = $"{docType} {number}";
        document.Info.Author = company.Name;

        var page = document.AddPage();
        page.Width = XUnit.FromMillimeter(148); // A5 width
        page.Height = XUnit.FromMillimeter(210); // A5 height
        var gfx = XGraphics.FromPdfPage(page);

        var fontRegular = new XFont("Arial", 8, XFontStyleEx.Regular);
        var fontBold = new XFont("Arial", 8, XFontStyleEx.Bold);
        var fontSmall = new XFont("Arial", 7, XFontStyleEx.Regular);
        var fontTitle = new XFont("Arial", 13, XFontStyleEx.Bold);
        var fontHeader = new XFont("Arial", 12, XFontStyleEx.Bold);
        var fontLabel = new XFont("Arial", 9, XFontStyleEx.Bold);

        double margin = 15;
        double y = margin;
        double pageWidth = page.Width.Point;
        double contentWidth = pageWidth - 2 * margin;

        // ---------- En-tete societe (avec logo) ----------
        double logoSize = 45;
        double textStartX = margin;
        try
        {
            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");
            if (File.Exists(logoPath))
            {
                using var logoImg = XImage.FromFile(logoPath);
                double ratio = logoImg.PixelHeight > 0 ? (double)logoImg.PixelWidth / logoImg.PixelHeight : 1;
                double logoW = logoSize * ratio;
                gfx.DrawImage(logoImg, margin, y, logoW, logoSize);
                textStartX = margin + logoW + 8;
            }
        }
        catch { /* logo optionnel : on continue sans si le fichier est absent/illisible */ }

        double textWidth = pageWidth - margin - textStartX;
        gfx.DrawString(company.Name.ToUpper(), fontHeader, XBrushes.Black, new XRect(textStartX, y, textWidth, 18), XStringFormats.TopLeft);
        y += 16;
        if (!string.IsNullOrEmpty(company.Address))
        {
            gfx.DrawString(company.Address, fontSmall, XBrushes.Black, new XRect(textStartX, y, textWidth, 12), XStringFormats.TopLeft);
            y += 10;
        }
        var contactLine = $"Tél: {company.Phone ?? ""}" + (string.IsNullOrEmpty(company.WhatsApp) ? "" : $"  WhatsApp: {company.WhatsApp}");
        gfx.DrawString(contactLine, fontSmall, XBrushes.Black, new XRect(textStartX, y, textWidth, 12), XStringFormats.TopLeft);
        y += 10;
        if (!string.IsNullOrEmpty(company.Nif) || !string.IsNullOrEmpty(company.Rccm))
        {
            gfx.DrawString($"NIF: {company.Nif ?? ""}  RCCM: {company.Rccm ?? ""}", fontSmall, XBrushes.Black, new XRect(textStartX, y, textWidth, 12), XStringFormats.TopLeft);
            y += 10;
        }
        y = Math.Max(y, margin + logoSize) + 4;

        // ---------- "DOIT:" + client ----------
        gfx.DrawString("DOIT:", fontLabel, XBrushes.Black, new XRect(margin, y, 40, 14), XStringFormats.TopLeft);
        gfx.DrawString(client.Name.ToUpper(), fontLabel, XBrushes.Black, new XRect(margin + 42, y, contentWidth - 42, 14), XStringFormats.TopLeft);
        y += 14;
        gfx.DrawLine(XPens.Black, margin, y, margin + contentWidth, y);
        y += 8;

        // ---------- Badge type de document ----------
        var badgeText = docType == "FACTURE" ? "FACTURE CLIENT" : docType;
        var badgeWidth = gfx.MeasureString(badgeText, fontTitle).Width + 20;
        var badgeRect = new XRect(margin, y, badgeWidth, 20);
        gfx.DrawRoundedRectangle(XPens.Black, XBrushes.White, badgeRect, new XSize(8, 8));
        gfx.DrawString(badgeText, fontTitle, XBrushes.Black, badgeRect, XStringFormats.Center);
        y += 28;

        // ---------- Tableau NUMERO / DATE / REFERENCE / HEURES (comme le modele papier) ----------
        double infoColWidth = contentWidth / 4;
        double infoRowHeight = 14;

        var infoHeaders = new[] { "NUMÉRO", "DATE", "RÉFÉRENCE", "HEURES" };
        var infoValues = new[] { number, date.ToString("dd/MM/yy"), "", date.ToString("HH:mm:ss") };

        for (int i = 0; i < 4; i++)
        {
            var cellRect = new XRect(margin + i * infoColWidth, y, infoColWidth, infoRowHeight);
            gfx.DrawRectangle(XPens.Black, cellRect);
            gfx.DrawString(infoHeaders[i], fontSmall, XBrushes.Black, cellRect, XStringFormats.Center);
        }
        y += infoRowHeight;
        for (int i = 0; i < 4; i++)
        {
            var cellRect = new XRect(margin + i * infoColWidth, y, infoColWidth, infoRowHeight);
            gfx.DrawRectangle(XPens.Black, cellRect);
            gfx.DrawString(infoValues[i], fontRegular, XBrushes.Black, cellRect, XStringFormats.Center);
        }
        y += infoRowHeight + 8;

        // ---------- Coordonnees client (complement) ----------
        var clientAddr = address ?? client.Address;
        var clientPhone = phone ?? client.Phone;
        if (!string.IsNullOrEmpty(clientAddr))
        {
            gfx.DrawString(clientAddr, fontRegular, XBrushes.Black, new XRect(margin, y, contentWidth, 12), XStringFormats.TopLeft);
            y += 11;
        }
        if (!string.IsNullOrEmpty(clientPhone))
        {
            gfx.DrawString($"Tél: {clientPhone}", fontRegular, XBrushes.Black, new XRect(margin, y, contentWidth, 12), XStringFormats.TopLeft);
            y += 11;
        }
        if (validUntil.HasValue)
        {
            var validLabel = showPayment ? "Échéance" : "Validité";
            gfx.DrawString($"{validLabel}: {validUntil.Value:dd/MM/yyyy}", fontSmall, XBrushes.Black, new XRect(margin, y, contentWidth, 12), XStringFormats.TopLeft);
            y += 11;
        }
        y += 6;

        // ---------- Tableau des lignes (Designation / Qte / Px unitaire / Montant HT) ----------
        double[] cols = { margin, margin + contentWidth - 190, margin + contentWidth - 130, margin + contentWidth - 65, pageWidth - margin };
        // cols[0]=debut Designation, cols[1]=debut Qte, cols[2]=debut Px unitaire, cols[3]=debut Montant, cols[4]=fin

        gfx.DrawRectangle(XPens.Black, new XRect(margin, y, contentWidth, 14));
        gfx.DrawLine(XPens.Black, cols[1], y, cols[1], y + 14);
        gfx.DrawLine(XPens.Black, cols[2], y, cols[2], y + 14);
        gfx.DrawLine(XPens.Black, cols[3], y, cols[3], y + 14);
        gfx.DrawString("Désignation", fontBold, XBrushes.Black, new XRect(cols[0] + 3, y, cols[1] - cols[0] - 3, 14), XStringFormats.CenterLeft);
        gfx.DrawString("Qté", fontBold, XBrushes.Black, new XRect(cols[1], y, cols[2] - cols[1], 14), XStringFormats.Center);
        gfx.DrawString("Px unitaire", fontBold, XBrushes.Black, new XRect(cols[2], y, cols[3] - cols[2], 14), XStringFormats.Center);
        gfx.DrawString("Montant HT", fontBold, XBrushes.Black, new XRect(cols[3], y, cols[4] - cols[3], 14), XStringFormats.Center);
        y += 14;

        foreach (var line in lines)
        {
            double rowHeight = 14;
            var designationText = string.IsNullOrEmpty(line.Ref) ? line.Designation : $"{line.Designation} ({line.Ref})";

            gfx.DrawRectangle(XPens.Gray, new XRect(margin, y, contentWidth, rowHeight));
            gfx.DrawLine(XPens.Gray, cols[1], y, cols[1], y + rowHeight);
            gfx.DrawLine(XPens.Gray, cols[2], y, cols[2], y + rowHeight);
            gfx.DrawLine(XPens.Gray, cols[3], y, cols[3], y + rowHeight);

            gfx.DrawString(designationText, fontRegular, XBrushes.Black, new XRect(cols[0] + 3, y, cols[1] - cols[0] - 6, rowHeight), XStringFormats.CenterLeft);
            gfx.DrawString(line.Qty.ToString("0.##"), fontRegular, XBrushes.Black, new XRect(cols[1], y, cols[2] - cols[1], rowHeight), XStringFormats.Center);
            gfx.DrawString(line.Price > 0 ? line.Price.ToString("N0") : "", fontRegular, XBrushes.Black, new XRect(cols[2], y, cols[3] - cols[2], rowHeight), XStringFormats.Center);
            gfx.DrawString(line.Total > 0 ? line.Total.ToString("N0") : "", fontRegular, XBrushes.Black, new XRect(cols[3], y, cols[4] - cols[3], rowHeight), XStringFormats.Center);
            y += rowHeight;
        }
        y += 10;

        // ---------- Montant en toutes lettres (comme le modele papier) ----------
        if (total > 0)
        {
            var docLabel = docType switch
            {
                "FACTURE" => "la présente facture",
                "DEVIS" => "le présent devis",
                _ => "le présent bon"
            };
            gfx.DrawString($"Arrêté {docLabel} à la somme de (en FCFA):", fontRegular, XBrushes.Black, new XRect(margin, y, contentWidth, 12), XStringFormats.TopLeft);
            y += 11;
            var fontItalic = new XFont("Arial", 8, XFontStyleEx.Italic);
            gfx.DrawString(NumberToWordsFr.ToWords(total), fontItalic, XBrushes.Black, new XRect(margin, y, contentWidth, 14), XStringFormats.TopLeft);
            y += 16;
        }

        // ---------- Cases de totaux encadrees (comme le modele papier) ----------
        double totalsBoxWidth = 150;
        double totalsLabelWidth = 95;
        double totalsX = pageWidth - margin - totalsBoxWidth;
        double totalsRowHeight = 16;

        void DrawTotalRow(string label, string value, bool bold)
        {
            var f = bold ? fontBold : fontRegular;
            var labelRect = new XRect(totalsX, y, totalsLabelWidth, totalsRowHeight);
            var valueRect = new XRect(totalsX + totalsLabelWidth, y, totalsBoxWidth - totalsLabelWidth, totalsRowHeight);
            gfx.DrawRectangle(XPens.Black, labelRect);
            gfx.DrawRectangle(XPens.Black, valueRect);
            gfx.DrawString(label, f, XBrushes.Black, labelRect, XStringFormats.CenterLeft);
            gfx.DrawString(value, f, XBrushes.Black, valueRect, XStringFormats.CenterRight);
            y += totalsRowHeight;
        }

        if (subTotal > 0 && (discount > 0 || taxAmount > 0))
            DrawTotalRow("Sous-total:", subTotal.ToString("N0"), false);
        if (discount > 0)
            DrawTotalRow("Remise:", discount.ToString("N0"), false);
        if (taxAmount > 0)
            DrawTotalRow($"Taxe ({taxRate}%):", taxAmount.ToString("N0"), false);
        if (total > 0)
            DrawTotalRow("Montant Total:", total.ToString("N0") + " FCFA", true);
        if (showPayment)
        {
            DrawTotalRow("Total Réglé:", paid.ToString("N0"), false);
            DrawTotalRow("Reste à payer:", balance.ToString("N0") + " FCFA", true);
        }
        y += 12;

        if (!string.IsNullOrEmpty(paymentTerms))
        {
            gfx.DrawString($"Conditions: {paymentTerms}", fontRegular, XBrushes.Black, new XRect(margin, y, contentWidth, 12), XStringFormats.TopLeft);
            y += 11;
        }
        if (!string.IsNullOrEmpty(notes))
        {
            gfx.DrawString($"Notes: {notes}", fontRegular, XBrushes.Black, new XRect(margin, y, contentWidth, 12), XStringFormats.TopLeft);
            y += 11;
        }

        // ---------- Signatures "Pour Acquit" / "Le Fournisseur" ----------
        double signatureY = page.Height.Point - margin - 55;
        if (signatureY > y) y = signatureY;

        gfx.DrawString("Pour Acquit", fontLabel, XBrushes.Black, new XRect(margin, y, contentWidth / 2, 14), XStringFormats.TopLeft);
        gfx.DrawString("Le Fournisseur", fontLabel, XBrushes.Black, new XRect(margin + contentWidth / 2, y, contentWidth / 2, 14), XStringFormats.TopRight);
        y += 14;
        gfx.DrawLine(XPens.Black, margin, y, margin + 90, y);
        gfx.DrawLine(XPens.Black, margin + contentWidth - 90, y, margin + contentWidth, y);

        // ---------- Pied de page ----------
        double footerY = page.Height.Point - margin - 20;
        gfx.DrawLine(XPens.Gray, margin, footerY, pageWidth - margin, footerY);
        footerY += 5;
        gfx.DrawString(company.DocumentFooter ?? "", fontSmall, XBrushes.Gray,
            new XRect(margin, footerY, pageWidth - 2 * margin, 15), XStringFormats.TopLeft);

        document.Save(filePath);
    }
}
