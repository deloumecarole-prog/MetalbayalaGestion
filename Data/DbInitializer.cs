using Microsoft.EntityFrameworkCore;
using MetalBayalaGestion.Models;
using MetalBayalaGestion.Services;
using System.Collections.Generic;
using System.Linq;

namespace MetalBayalaGestion.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context, bool withDemoData = false)
    {
        // Aucun dossier "Migrations" n'existe dans le projet : Migrate() ne créerait aucune table.
        // EnsureCreated() génère le schéma directement à partir des modèles.
        context.Database.EnsureCreated();

        if (!context.Users.Any(u => u.Username == "admin"))
        {
            context.Users.Add(new User
            {
                Username = "admin",
                PasswordHash = PasswordHasher.HashPassword("admin123"),
                Role = "Administrateur",
                FullName = "Administrateur",
                IsActive = true
            });
            context.SaveChanges();
        }

        if (!context.Companies.Any())
        {
            context.Companies.Add(new Company
            {
                Name = "Metal Bayala",
                Address = "Route de Banankabougou terminus, BAMAKO / MALI",
                Phone = "75 50 50 25 / 77 77 72 75 / 66 53 66 66",
                Nif = "08613189E",
                Currency = "FCFA",
                CurrencyCode = "XOF"
            });
            context.SaveChanges();
        }

        if (withDemoData && !context.Clients.Any())
        {
            SeedDemoData(context);
        }
    }

    private static void SeedDemoData(AppDbContext context)
    {
        var clients = new List<Client>
        {
            new() { Code = "CLI-001", Name = "SARL Construction Mali", Contact = "Moussa Diallo", Phone = "+223 70 00 00 01", City = "Bamako", Address = "Hamdallaye ACI 2000" },
            new() { Code = "CLI-002", Name = "Entreprise Traore", Contact = "Amadou Traore", Phone = "+223 70 00 00 02", City = "Bamako", Address = "Badalabougou" },
            new() { Code = "CLI-003", Name = "BTP Ségou", Contact = "Fatoumata Keita", Phone = "+223 70 00 00 03", City = "Ségou", Address = "Centre ville" },
            new() { Code = "CLI-004", Name = "Maison Dupont Mali", Contact = "Jean Dupont", Phone = "+223 70 00 00 04", City = "Bamako", Address = "Niaréla" },
            new() { Code = "CLI-005", Name = "SOTRAMA Construction", Contact = "Ibrahim Sissoko", Phone = "+223 70 00 00 05", City = "Bamako", Address = "Médina Coura" }
        };
        context.Clients.AddRange(clients);
        context.SaveChanges();

        var categories = new List<Category>
        {
            new() { Name = "Fer et Acier" },
            new() { Name = "Ciment et Matériaux" },
            new() { Name = "Outillage" }
        };
        context.Categories.AddRange(categories);
        context.SaveChanges();

        var products = new List<Product>
        {
            new() { Reference = "REF-001", Code = "P001", Designation = "Fer à béton 8mm", CategoryId = 1, PurchasePrice = 250000, SalePrice = 280000, Unit = "Tonne", StockQuantity = 15, MinStock = 5 },
            new() { Reference = "REF-002", Code = "P002", Designation = "Fer à béton 10mm", CategoryId = 1, PurchasePrice = 245000, SalePrice = 275000, Unit = "Tonne", StockQuantity = 12, MinStock = 5 },
            new() { Reference = "REF-003", Code = "P003", Designation = "Fer à béton 12mm", CategoryId = 1, PurchasePrice = 240000, SalePrice = 270000, Unit = "Tonne", StockQuantity = 8, MinStock = 5 },
            new() { Reference = "REF-004", Code = "P004", Designation = "Tole ondulée 2m", CategoryId = 1, PurchasePrice = 15000, SalePrice = 18000, Unit = "Feuille", StockQuantity = 200, MinStock = 50 },
            new() { Reference = "REF-005", Code = "P005", Designation = "Tole ondulée 3m", CategoryId = 1, PurchasePrice = 22000, SalePrice = 26000, Unit = "Feuille", StockQuantity = 150, MinStock = 50 },
            new() { Reference = "REF-006", Code = "P006", Designation = "Ciment CPJ 42.5", CategoryId = 2, PurchasePrice = 6500, SalePrice = 7500, Unit = "Sac", StockQuantity = 500, MinStock = 100 },
            new() { Reference = "REF-007", Code = "P007", Designation = "Ciment CPJ 32.5", CategoryId = 2, PurchasePrice = 6000, SalePrice = 7000, Unit = "Sac", StockQuantity = 300, MinStock = 100 },
            new() { Reference = "REF-008", Code = "P008", Designation = "Sable fin", CategoryId = 2, PurchasePrice = 35000, SalePrice = 40000, Unit = "m3", StockQuantity = 50, MinStock = 20 },
            new() { Reference = "REF-009", Code = "P009", Designation = "Marteau de maçon", CategoryId = 3, PurchasePrice = 3500, SalePrice = 5000, Unit = "Pièce", StockQuantity = 30, MinStock = 10 },
            new() { Reference = "REF-010", Code = "P010", Designation = "Truelle", CategoryId = 3, PurchasePrice = 2000, SalePrice = 3000, Unit = "Pièce", StockQuantity = 25, MinStock = 10 }
        };
        context.Products.AddRange(products);
        context.SaveChanges();

        var quote1 = new Quote
        {
            Number = "DEV-2026-0001",
            Date = System.DateTime.Now.AddDays(-5),
            ValidUntil = System.DateTime.Now.AddDays(25),
            ClientId = 1,
            ClientAddress = clients[0].Address,
            ClientPhone = clients[0].Phone,
            Status = "Accepté",
            SubTotal = 560000,
            Discount = 0,
            TaxRate = 0,
            TaxAmount = 0,
            Total = 560000,
            PaymentTerms = "30 jours",
            Notes = "Devis pour construction villa"
        };
        context.Quotes.Add(quote1);
        context.SaveChanges();

        context.QuoteLines.AddRange(new List<QuoteLine>
        {
            new() { QuoteId = quote1.Id, ProductId = 1, ProductReference = "REF-001", Designation = "Fer à béton 8mm", Quantity = 2, Unit = "Tonne", UnitPrice = 280000, Discount = 0 },
        });
        context.SaveChanges();

        var quote2 = new Quote
        {
            Number = "DEV-2026-0002",
            Date = System.DateTime.Now.AddDays(-3),
            ValidUntil = System.DateTime.Now.AddDays(27),
            ClientId = 2,
            ClientAddress = clients[1].Address,
            ClientPhone = clients[1].Phone,
            Status = "Brouillon",
            SubTotal = 75000,
            Discount = 5000,
            TaxRate = 0,
            TaxAmount = 0,
            Total = 70000,
            Notes = "Matériaux divers"
        };
        context.Quotes.Add(quote2);
        context.SaveChanges();

        context.QuoteLines.AddRange(new List<QuoteLine>
        {
            new() { QuoteId = quote2.Id, ProductId = 6, ProductReference = "REF-006", Designation = "Ciment CPJ 42.5", Quantity = 10, Unit = "Sac", UnitPrice = 7500, Discount = 5000 },
        });
        context.SaveChanges();

        var quote3 = new Quote
        {
            Number = "DEV-2026-0003",
            Date = System.DateTime.Now.AddDays(-1),
            ValidUntil = System.DateTime.Now.AddDays(29),
            ClientId = 3,
            ClientAddress = clients[2].Address,
            ClientPhone = clients[2].Phone,
            Status = "Envoyé",
            SubTotal = 18000,
            Discount = 0,
            TaxRate = 0,
            TaxAmount = 0,
            Total = 18000,
            Notes = "Tole ondulée"
        };
        context.Quotes.Add(quote3);
        context.SaveChanges();

        context.QuoteLines.AddRange(new List<QuoteLine>
        {
            new() { QuoteId = quote3.Id, ProductId = 4, ProductReference = "REF-004", Designation = "Tole ondulée 2m", Quantity = 1, Unit = "Feuille", UnitPrice = 18000, Discount = 0 },
        });
        context.SaveChanges();

        var invoice1 = new Invoice
        {
            Number = "FAC-2026-0001",
            Date = System.DateTime.Now.AddDays(-10),
            DueDate = System.DateTime.Now.AddDays(20),
            ClientId = 1,
            ClientAddress = clients[0].Address,
            ClientPhone = clients[0].Phone,
            Status = "Payée",
            SubTotal = 560000,
            Discount = 0,
            TaxRate = 0,
            TaxAmount = 0,
            Total = 560000,
            PaidAmount = 560000,
            QuoteId = quote1.Id,
            Notes = "Facture suite devis DEV-2026-0001"
        };
        context.Invoices.Add(invoice1);
        context.SaveChanges();

        context.InvoiceLines.AddRange(new List<InvoiceLine>
        {
            new() { InvoiceId = invoice1.Id, ProductId = 1, ProductReference = "REF-001", Designation = "Fer à béton 8mm", Quantity = 2, Unit = "Tonne", UnitPrice = 280000, Discount = 0 },
        });
        context.SaveChanges();

        var invoice2 = new Invoice
        {
            Number = "FAC-2026-0002",
            Date = System.DateTime.Now.AddDays(-5),
            DueDate = System.DateTime.Now.AddDays(25),
            ClientId = 2,
            ClientAddress = clients[1].Address,
            ClientPhone = clients[1].Phone,
            Status = "Partiellement payée",
            SubTotal = 75000,
            Discount = 5000,
            TaxRate = 0,
            TaxAmount = 0,
            Total = 70000,
            PaidAmount = 35000,
            Notes = "Acompte reçu"
        };
        context.Invoices.Add(invoice2);
        context.SaveChanges();

        context.InvoiceLines.AddRange(new List<InvoiceLine>
        {
            new() { InvoiceId = invoice2.Id, ProductId = 6, ProductReference = "REF-006", Designation = "Ciment CPJ 42.5", Quantity = 10, Unit = "Sac", UnitPrice = 7500, Discount = 5000 },
        });
        context.SaveChanges();

        var invoice3 = new Invoice
        {
            Number = "FAC-2026-0003",
            Date = System.DateTime.Now.AddDays(-2),
            DueDate = System.DateTime.Now.AddDays(28),
            ClientId = 3,
            ClientAddress = clients[2].Address,
            ClientPhone = clients[2].Phone,
            Status = "Impayée",
            SubTotal = 18000,
            Discount = 0,
            TaxRate = 0,
            TaxAmount = 0,
            Total = 18000,
            PaidAmount = 0,
            Notes = "Urgent"
        };
        context.Invoices.Add(invoice3);
        context.SaveChanges();

        context.InvoiceLines.AddRange(new List<InvoiceLine>
        {
            new() { InvoiceId = invoice3.Id, ProductId = 4, ProductReference = "REF-004", Designation = "Tole ondulée 2m", Quantity = 1, Unit = "Feuille", UnitPrice = 18000, Discount = 0 },
        });
        context.SaveChanges();

        // Payments
        context.Payments.AddRange(new List<Payment>
        {
            new() { Number = "PAY-2026-0001", Date = System.DateTime.Now.AddDays(-9), ClientId = 1, InvoiceId = invoice1.Id, Amount = 560000, Mode = "Virement bancaire", Reference = "VIR-001", Notes = "Paiement total" },
            new() { Number = "PAY-2026-0002", Date = System.DateTime.Now.AddDays(-3), ClientId = 2, InvoiceId = invoice2.Id, Amount = 35000, Mode = "Orange Money", Reference = "OM-12345", Notes = "Acompte" },
        });
        context.SaveChanges();

        // Update client balances
        // .AsEnumerable() : SQLite ne peut pas traduire Sum() sur un decimal en SQL directement.
        foreach (var c in context.Clients)
        {
            c.TotalInvoiced = context.Invoices.Where(i => i.ClientId == c.Id && !i.IsDeleted).AsEnumerable().Sum(i => i.Total);
            c.TotalPaid = context.Payments.Where(p => p.ClientId == c.Id && !p.IsDeleted).AsEnumerable().Sum(p => p.Amount);
        }
        context.SaveChanges();

        // Cash transactions
        context.CashTransactions.AddRange(new List<CashTransaction>
        {
            new() { Date = System.DateTime.Now.AddDays(-9), Label = "Paiement facture FAC-2026-0001", Category = "Encaissement client", Type = "Entrée", Amount = 560000, Mode = "Virement bancaire", Reference = "VIR-001" },
            new() { Date = System.DateTime.Now.AddDays(-3), Label = "Paiement facture FAC-2026-0002", Category = "Encaissement client", Type = "Entrée", Amount = 35000, Mode = "Orange Money", Reference = "OM-12345" },
            new() { Date = System.DateTime.Now.AddDays(-7), Label = "Transport livraison", Category = "Transport", Type = "Sortie", Amount = 15000, Mode = "Espèces", Reference = "TR-01" },
            new() { Date = System.DateTime.Now.AddDays(-2), Label = "Fournitures bureau", Category = "Fournitures", Type = "Sortie", Amount = 25000, Mode = "Espèces", Reference = "FB-01" },
        });
        context.SaveChanges();

        // Expenses
        context.Expenses.AddRange(new List<Expense>
        {
            new() { Category = "Transport", Description = "Transport livraison Bamako", Amount = 15000, Date = System.DateTime.Now.AddDays(-7), Mode = "Espèces" },
            new() { Category = "Fournitures", Description = "Fournitures bureau", Amount = 25000, Date = System.DateTime.Now.AddDays(-2), Mode = "Espèces" },
            new() { Category = "Électricité", Description = "Facture électricité juillet", Amount = 45000, Date = System.DateTime.Now.AddDays(-15), Mode = "Virement bancaire" },
        });
        context.SaveChanges();

        // Stock movements
        context.StockMovements.AddRange(new List<StockMovement>
        {
            new() { ProductId = 1, Type = "Sortie", Quantity = 2, UnitPrice = 280000, Reference = "FAC-2026-0001", Notes = "Vente" },
            new() { ProductId = 6, Type = "Sortie", Quantity = 10, UnitPrice = 7500, Reference = "FAC-2026-0002", Notes = "Vente" },
            new() { ProductId = 4, Type = "Sortie", Quantity = 1, UnitPrice = 18000, Reference = "FAC-2026-0003", Notes = "Vente" },
        });
        context.SaveChanges();
    }
}
