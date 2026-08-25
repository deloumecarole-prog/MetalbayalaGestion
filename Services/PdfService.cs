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
        List<StockMovement> stockMovements, List<Product> lowStockProducts,
        List<Invoice> unpaidInvoices, List<Expense> expenses, List<Payment> payments,
        decimal cashCounted, decimal dailyGap, string filePath)
    {
        await Task.Run(() =>
        {
            var company = _context.Companies.FirstOrDefault() ?? new Company();
            var document = new PdfDocument();
            document.Info.Title = $"Rapport {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
            document.Info.Author = company.Name;

            var fontRegular = new XFont("Arial", 8, XFontStyleEx.Regular);
            var fontBold = new XFont("Arial", 8, XFontStyleEx.Bold);
            var fontSection = new XFont("Arial", 11, XFontStyleEx.Bold);

            double margin = 25;
            double pageWidth = 0, pageHeight = 0, contentWidth = 0, y = 0;
            PdfPage page = null!;
            XGraphics gfx = null!;
            int pageNumber = 0;
            var docRef = $"RAP-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}";

            var fontKpiValue = new XFont("Arial", 30, XFontStyleEx.Bold);
            var fontKpiLabel = new XFont("Arial", 11, XFontStyleEx.Regular);
            var fontBigTitle = new XFont("Arial", 24, XFontStyleEx.Bold);

            // Cree une nouvelle page, tamponne la precedente (numero de page inclus)
            // avant de basculer dessus. Sans cette gestion de pagination, un rapport
            // avec beaucoup de lignes (factures impayees, depenses, etc.) coupait
            // simplement en bas de la premiere page et le reste disparaissait.
            // "landscape" ne s'applique qu'a la premiere page de couverture (chiffres
            // cles en gros) ; toutes les pages de detail qui suivent restent en portrait.
            void NewPage(bool landscape = false)
            {
                if (page != null)
                    DrawFooterStamp(gfx, $"{docRef}  •  Page {pageNumber}", margin, pageWidth, pageHeight);

                page = document.AddPage();
                page.Width = XUnit.FromMillimeter(landscape ? 297 : 210);
                page.Height = XUnit.FromMillimeter(landscape ? 210 : 297);
                gfx = XGraphics.FromPdfPage(page);
                pageWidth = page.Width.Point;
                pageHeight = page.Height.Point;
                contentWidth = pageWidth - 2 * margin;
                pageNumber++;
                y = margin;
            }

            // Passe a la page suivante (portrait) si l'espace restant est insuffisant
            // pour "needed" points de hauteur (garde toujours de la place pour le pied de page).
            bool EnsureSpace(double needed)
            {
                if (y + needed > pageHeight - margin - 20)
                {
                    NewPage();
                    return true;
                }
                return false;
            }

            // ---------- Page de couverture (paysage, chiffres cles en gros) ----------
            NewPage(landscape: true);

            y = DrawCompanyHeader(gfx, company, margin, pageWidth, y);
            y += 10;
            gfx.DrawString("RAPPORT D'ACTIVITÉ", fontBigTitle, XBrushes.Black, new XRect(margin, y, contentWidth, 32), XStringFormats.TopCenter);
            y += 30;
            gfx.DrawString($"Période : {startDate:dd/MM/yyyy} au {endDate:dd/MM/yyyy}", fontKpiLabel, XBrushes.Black, new XRect(margin, y, contentWidth, 16), XStringFormats.TopCenter);
            y += 35;

            var kpis = new (string Label, decimal Value, XBrush Brush)[]
            {
                ("CHIFFRE D'AFFAIRES", totalSales, XBrushes.Black),
                ("TOTAL ENCAISSÉ", totalCashIn, XBrushes.Black),
                ("CRÉANCES", totalReceivables, XBrushes.DarkRed),
                ("DÉPENSES", totalExpenses, XBrushes.Black),
                ("CAISSE NETTE (THÉORIQUE)", cashBalance, XBrushes.DarkGreen),
                ("CAISSE COMPTÉE (JOUR)", cashCounted, XBrushes.Black),
                ("ÉCART DU JOUR", dailyGap, dailyGap == 0 ? XBrushes.DarkGreen : XBrushes.DarkRed),
            };

            int kpiCols = 4;
            double kpiGap = 12;
            double kpiBoxWidth = (contentWidth - (kpiCols - 1) * kpiGap) / kpiCols;
            double kpiBoxHeight = 90;

            for (int i = 0; i < kpis.Length; i++)
            {
                int row = i / kpiCols;
                int col = i % kpiCols;
                double bx = margin + col * (kpiBoxWidth + kpiGap);
                double by = y + row * (kpiBoxHeight + kpiGap);

                var boxRect = new XRect(bx, by, kpiBoxWidth, kpiBoxHeight);
                gfx.DrawRoundedRectangle(XPens.Gray, XBrushes.WhiteSmoke, boxRect, new XSize(6, 6));
                gfx.DrawString(kpis[i].Label, fontKpiLabel, XBrushes.Gray, new XRect(bx + 8, by + 10, kpiBoxWidth - 16, 16), XStringFormats.TopLeft);
                gfx.DrawString(kpis[i].Value.ToString("N0"), fontKpiValue, kpis[i].Brush, new XRect(bx + 8, by + 32, kpiBoxWidth - 16, 40), XStringFormats.TopLeft);
                gfx.DrawString("FCFA", fontKpiLabel, XBrushes.Gray, new XRect(bx + 8, by + 68, kpiBoxWidth - 16, 14), XStringFormats.TopLeft);
            }

            // ---------- Pages de detail (portrait) ----------
            NewPage(landscape: false);
            gfx.DrawString("DÉTAIL DES OPÉRATIONS", fontSection, XBrushes.Black, new XRect(margin, y, contentWidth, 18), XStringFormats.TopLeft);
            y += 22;

            // Dessine une section tabulaire generique (titre + entetes + lignes), avec
            // saut de page automatique et re-affichage des entetes de colonnes sur
            // chaque nouvelle page pour que le tableau reste lisible partout.
            void DrawSection<T>(string title, List<T> items, (string Header, double Width, bool Right, Func<T, string> Value)[] columns)
            {
                EnsureSpace(45);
                gfx.DrawString(title.ToUpper(), fontSection, XBrushes.Black, new XRect(margin, y, contentWidth, 15), XStringFormats.TopLeft);
                y += 16;
                gfx.DrawLine(XPens.Black, margin, y, pageWidth - margin, y);
                y += 5;

                void DrawHeaderRow()
                {
                    double x = margin;
                    foreach (var c in columns)
                    {
                        gfx.DrawString(c.Header, fontBold, XBrushes.Black, new XRect(x, y, c.Width, 12),
                            c.Right ? XStringFormats.TopRight : XStringFormats.TopLeft);
                        x += c.Width;
                    }
                    y += 12;
                    gfx.DrawLine(XPens.Gray, margin, y, pageWidth - margin, y);
                    y += 4;
                }

                DrawHeaderRow();

                if (items.Count == 0)
                {
                    gfx.DrawString("(aucune donnée)", fontRegular, XBrushes.Gray, new XRect(margin, y, contentWidth, 12), XStringFormats.TopLeft);
                    y += 12;
                }

                foreach (var item in items)
                {
                    if (EnsureSpace(11))
                        DrawHeaderRow();

                    double x = margin;
                    foreach (var c in columns)
                    {
                        gfx.DrawString(c.Value(item) ?? "", fontRegular, XBrushes.Black, new XRect(x, y, c.Width, 11),
                            c.Right ? XStringFormats.TopRight : XStringFormats.TopLeft);
                        x += c.Width;
                    }
                    y += 11;
                }
                y += 14;
            }

            DrawSection("Factures impayées", unpaidInvoices, new (string, double, bool, Func<Invoice, string>)[]
            {
                ("N° pièce", 75, false, i => i.Number),
                ("Date", 55, false, i => i.Date.ToString("dd/MM/yy")),
                ("Client", 150, false, i => i.Client?.Name ?? ""),
                ("Montant", 80, true, i => i.Total.ToString("N0")),
                ("Acompte", 75, true, i => i.PaidAmount.ToString("N0")),
                ("Reste", 75, true, i => i.Balance.ToString("N0")),
                ("Statut", 35, false, i => i.Status),
            });

            DrawSection("Dépenses", expenses, new (string, double, bool, Func<Expense, string>)[]
            {
                ("Date", 50, false, e => e.Date.ToString("dd/MM/yy")),
                ("Catégorie", 90, false, e => e.Category),
                ("Description", 150, false, e => e.Description),
                ("Fournisseur", 100, false, e => e.Supplier?.Name ?? ""),
                ("Montant", 75, true, e => e.Amount.ToString("N0")),
                ("Mode", 80, false, e => e.Mode),
            });

            DrawSection("Journal des règlements", payments, new (string, double, bool, Func<Payment, string>)[]
            {
                ("N° pièce", 75, false, p => p.Number),
                ("Date", 55, false, p => p.Date.ToString("dd/MM/yy")),
                ("Client", 150, false, p => p.Client?.Name ?? ""),
                ("Mode", 90, false, p => p.Mode),
                ("Référence", 90, false, p => p.Reference ?? ""),
                ("Montant", 85, true, p => p.Amount.ToString("N0")),
            });

            DrawSection("Mouvements de stock", stockMovements, new (string, double, bool, Func<StockMovement, string>)[]
            {
                ("Date", 80, false, m => m.CreatedAt.ToString("dd/MM/yy HH:mm")),
                ("Produit", 180, false, m => m.Product?.Designation ?? ""),
                ("Type", 60, false, m => m.Type),
                ("Qté", 60, true, m => m.Quantity.ToString("0.##")),
                ("Référence", 165, false, m => m.Reference ?? ""),
            });

            DrawSection("Produits en stock bas", lowStockProducts, new (string, double, bool, Func<Product, string>)[]
            {
                ("Référence", 100, false, p => p.Reference),
                ("Désignation", 250, false, p => p.Designation),
                ("Stock", 100, true, p => p.StockQuantity.ToString("0.##")),
                ("Min", 95, true, p => p.MinStock.ToString("0.##")),
            });

            DrawFooterStamp(gfx, $"{docRef}  •  Page {pageNumber}", margin, pageWidth, pageHeight);

            document.Save(filePath);
        });
    }

    // Rapport de cloture de caisse : caisse theorique, montant compte, ecart,
    // avec zone de signature "Caissier" / "Responsable" pour formaliser le
    // controle de caisse quotidien (remplace le pointage papier manuel).
    public async Task GenerateCashClosingPdfAsync(DateTime date, decimal theoretical, decimal counted, decimal gap, string filePath)
    {
        await Task.Run(() =>
        {
            var company = _context.Companies.FirstOrDefault() ?? new Company();
            var document = new PdfDocument();
            document.Info.Title = $"Clôture de caisse {date:dd/MM/yyyy}";
            document.Info.Author = company.Name;

            var page = document.AddPage();
            page.Width = XUnit.FromMillimeter(148); // A5
            page.Height = XUnit.FromMillimeter(210);
            var gfx = XGraphics.FromPdfPage(page);

            var fontRegular = new XFont("Arial", 9, XFontStyleEx.Regular);
            var fontBold = new XFont("Arial", 9, XFontStyleEx.Bold);
            var fontTitle = new XFont("Arial", 13, XFontStyleEx.Bold);
            var fontLabel = new XFont("Arial", 9, XFontStyleEx.Bold);

            double margin = 15;
            double y = margin;
            double pageWidth = page.Width.Point;
            double pageHeight = page.Height.Point;
            double contentWidth = pageWidth - 2 * margin;

            y = DrawCompanyHeader(gfx, company, margin, pageWidth, y);
            y += 6;

            var badgeText = "CLÔTURE DE CAISSE";
            var badgeWidth = gfx.MeasureString(badgeText, fontTitle).Width + 20;
            var badgeRect = new XRect(margin, y, badgeWidth, 20);
            gfx.DrawRoundedRectangle(XPens.Black, XBrushes.White, badgeRect, new XSize(8, 8));
            gfx.DrawString(badgeText, fontTitle, XBrushes.Black, badgeRect, XStringFormats.Center);
            y += 30;

            gfx.DrawString($"Date : {date:dd/MM/yyyy}", fontRegular, XBrushes.Black, new XRect(margin, y, contentWidth, 14), XStringFormats.TopLeft);
            y += 25;

            double totalsBoxWidth = contentWidth;
            double totalsLabelWidth = totalsBoxWidth - 100;
            double totalsRowHeight = 20;

            void DrawRow(string label, decimal value, bool bold, XBrush? valueBrush = null)
            {
                var f = bold ? fontBold : fontRegular;
                var labelRect = new XRect(margin, y, totalsLabelWidth, totalsRowHeight);
                var valueRect = new XRect(margin + totalsLabelWidth, y, totalsBoxWidth - totalsLabelWidth, totalsRowHeight);
                gfx.DrawRectangle(XPens.Black, labelRect);
                gfx.DrawRectangle(XPens.Black, valueRect);
                gfx.DrawString(label, f, XBrushes.Black, labelRect, XStringFormats.CenterLeft);
                gfx.DrawString(value.ToString("N0") + " FCFA", f, valueBrush ?? XBrushes.Black, valueRect, XStringFormats.CenterRight);
                y += totalsRowHeight;
            }

            DrawRow("Caisse théorique (calculée)", theoretical, false);
            DrawRow("Caisse comptée (physique)", counted, false);
            DrawRow("Écart", gap, true, gap == 0 ? XBrushes.DarkGreen : XBrushes.DarkRed);
            y += 20;

            if (gap != 0)
            {
                var gapLabel = gap > 0 ? "Manquant en caisse" : "Excédent en caisse";
                gfx.DrawString($"({gapLabel} : {Math.Abs(gap):N0} FCFA)", fontRegular, gap > 0 ? XBrushes.DarkRed : XBrushes.DarkGreen,
                    new XRect(margin, y, contentWidth, 14), XStringFormats.TopLeft);
                y += 25;
            }

            // ---------- Signatures ----------
            double signatureY = pageHeight - margin - 55;
            if (signatureY > y) y = signatureY;

            gfx.DrawString("Caissier", fontLabel, XBrushes.Black, new XRect(margin, y, contentWidth / 2, 14), XStringFormats.TopLeft);
            gfx.DrawString("Responsable", fontLabel, XBrushes.Black, new XRect(margin + contentWidth / 2, y, contentWidth / 2, 14), XStringFormats.TopRight);
            y += 14;
            gfx.DrawLine(XPens.Black, margin, y, margin + 90, y);
            gfx.DrawLine(XPens.Black, margin + contentWidth - 90, y, margin + contentWidth, y);

            var docRef = $"CLOT-{date:yyyyMMdd}";
            DrawFooterStamp(gfx, docRef, margin, pageWidth, pageHeight);

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

    // En-tete societe reutilisable (logo + nom + adresse + contact + NIF/RCCM),
    // utilise sur tous les documents (devis/factures/BL, rapports, cloture de caisse)
    // pour un rendu uniforme et professionnel. Retourne la position Y apres l'en-tete.
    private double DrawCompanyHeader(XGraphics gfx, Company company, double margin, double pageWidth, double y, double logoSize = 45)
    {
        var fontHeader = new XFont("Arial", 12, XFontStyleEx.Bold);
        var fontSmall = new XFont("Arial", 7, XFontStyleEx.Regular);
        double startY = y;
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
        return Math.Max(y, startY + logoSize) + 4;
    }

    // Pied de page uniforme "reference document + date/heure de generation", pour
    // donner un caractere officiel/archivable a chaque rapport imprime (le meme
    // rapport regenere plus tard porte un tampon different, ce qui aide a tracer
    // quelle version a ete remise a qui).
    private void DrawFooterStamp(XGraphics gfx, string docRef, double margin, double pageWidth, double pageHeight)
    {
        var fontSmall = new XFont("Arial", 7, XFontStyleEx.Regular);
        double footerY = pageHeight - margin - 12;
        gfx.DrawString($"{docRef}  •  Généré le {DateTime.Now:dd/MM/yyyy HH:mm}", fontSmall, XBrushes.Gray,
            new XRect(margin, footerY, pageWidth - 2 * margin, 12), XStringFormats.TopLeft);
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
        y = DrawCompanyHeader(gfx, company, margin, pageWidth, y);

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
