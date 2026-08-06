using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalBayalaGestion.Data;
using MetalBayalaGestion.Models;
using MetalBayalaGestion.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MetalBayalaGestion.ViewModels;

public partial class InvoicesViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Invoice> _invoices = new();

    [ObservableProperty]
    private Invoice? _selectedInvoice;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _statusFilter = "Tous";

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;
    private readonly INumberingService _numberingService;
    private readonly IPdfService _pdfService;
    private readonly ICurrentUserService? _currentUserService;

    public InvoicesViewModel(AppDbContext context, IDialogService dialogService, INumberingService numberingService, IPdfService pdfService, ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _dialogService = dialogService;
        _numberingService = numberingService;
        _pdfService = pdfService;
        _currentUserService = currentUserService;
        LoadInvoices();
    }

    partial void OnSearchTextChanged(string value) => FilterInvoices();
    partial void OnStatusFilterChanged(string value) => FilterInvoices();

    private void LoadInvoices()
    {
        Invoices.Clear();
        var list = _context.Invoices.Where(i => !i.IsDeleted).Include(i => i.Client).OrderByDescending(i => i.Date).ToList();
        foreach (var i in list) Invoices.Add(i);
    }

    private void FilterInvoices()
    {
        Invoices.Clear();
        var query = _context.Invoices.Where(i => !i.IsDeleted).Include(i => i.Client).AsQueryable();
        if (StatusFilter != "Tous")
            query = query.Where(i => i.Status == StatusFilter);
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.ToLower();
            query = query.Where(i => i.Number.ToLower().Contains(s) || i.Client.Name.ToLower().Contains(s));
        }
        foreach (var i in query.OrderByDescending(i => i.Date).ToList()) Invoices.Add(i);
    }

    [RelayCommand]
    private async Task AddInvoice()
    {
        var number = await _numberingService.GetNextInvoiceNumberAsync();
        var vm = new InvoiceEditViewModel(new Invoice { Number = number, Date = DateTime.Now, DueDate = DateTime.Now.AddDays(30), Status = "Brouillon" }, _context, _dialogService);
        var window = new Views.InvoiceEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            // La vente reste possible même sans stock suffisant (le stock peut passer en négatif).
            // On collecte juste la liste des ruptures pour informer l'utilisateur après coup, sans bloquer.
            var shortages = new List<string>();
            foreach (var line in vm.Lines)
            {
                var product = await _context.Products.FindAsync(line.ProductId);
                if (product != null && product.StockQuantity < line.Quantity)
                    shortages.Add($"- {product.Designation} : {product.StockQuantity:0.##} {product.Unit} en stock, {line.Quantity:0.##} vendus");
            }

            _context.Invoices.Add(vm.Invoice);
            foreach (var line in vm.Lines)
            {
                line.Invoice = vm.Invoice; // navigation : EF Core resout InvoiceId apres l'insertion de la facture
                _context.InvoiceLines.Add(line);
            }
            await _context.SaveChangesAsync();

            // Décrémentation stock + mouvement si facture validée (non brouillon)
            if (vm.Invoice.Status != "Brouillon")
            {
                foreach (var line in vm.Lines)
                {
                    var product = await _context.Products.FindAsync(line.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= line.Quantity;
                        _context.StockMovements.Add(new StockMovement
                        {
                            ProductId = product.Id,
                            Type = "Sortie",
                            Quantity = line.Quantity,
                            UnitPrice = line.UnitPrice,
                            Reference = vm.Invoice.Number,
                            Notes = $"Vente - Facture {vm.Invoice.Number}"
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            LoadInvoices();

            if (shortages.Any())
                await _dialogService.ShowInfoAsync("Vente enregistrée - stock négatif",
                    "La facture a été enregistrée. Attention, ces articles sont désormais en stock négatif :\n" + string.Join("\n", shortages));
        }
    }

    [RelayCommand]
    private async Task EditInvoice()
    {
        if (SelectedInvoice == null) return;
        var originalStatus = SelectedInvoice.Status;
        var vm = new InvoiceEditViewModel(SelectedInvoice, _context, _dialogService);
        var window = new Views.InvoiceEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            // Si passage Brouillon -> autre statut : décrémenter (sans bloquer si stock insuffisant)
            var shortages = new List<string>();
            if (originalStatus == "Brouillon" && vm.Invoice.Status != "Brouillon")
            {
                foreach (var line in vm.Lines)
                {
                    var product = await _context.Products.FindAsync(line.ProductId);
                    if (product != null && product.StockQuantity < line.Quantity)
                        shortages.Add($"- {product.Designation} : {product.StockQuantity:0.##} {product.Unit} en stock, {line.Quantity:0.##} vendus");
                }

                foreach (var line in vm.Lines)
                {
                    var product = await _context.Products.FindAsync(line.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= line.Quantity;
                        _context.StockMovements.Add(new StockMovement
                        {
                            ProductId = product.Id,
                            Type = "Sortie",
                            Quantity = line.Quantity,
                            UnitPrice = line.UnitPrice,
                            Reference = vm.Invoice.Number,
                            Notes = $"Vente - Facture {vm.Invoice.Number}"
                        });
                    }
                }
            }

            _context.Invoices.Update(vm.Invoice);
            var existing = _context.InvoiceLines.Where(l => l.InvoiceId == vm.Invoice.Id).ToList();
            _context.InvoiceLines.RemoveRange(existing);
            foreach (var line in vm.Lines)
            {
                line.InvoiceId = vm.Invoice.Id;
                _context.InvoiceLines.Add(line);
            }
            await _context.SaveChangesAsync();
            LoadInvoices();

            if (shortages.Any())
                await _dialogService.ShowInfoAsync("Vente enregistrée - stock négatif",
                    "La facture a été enregistrée. Attention, ces articles sont désormais en stock négatif :\n" + string.Join("\n", shortages));
        }
    }

    [RelayCommand]
    private async Task DeleteInvoice()
    {
        if (SelectedInvoice == null) return;
        if (!await _dialogService.ShowConfirmAsync("Confirmation", $"Supprimer la facture {SelectedInvoice.Number} ?")) return;
        SelectedInvoice.IsDeleted = true;
        await _context.SaveChangesAsync();
        LoadInvoices();
    }

    [RelayCommand]
    private async Task PrintInvoice()
    {
        if (SelectedInvoice == null) return;
        var path = await _dialogService.ShowSaveFileDialogAsync("Exporter PDF", "PDF|*.pdf", $"{SelectedInvoice.Number}.pdf");
        if (path != null)
        {
            await _pdfService.GenerateInvoicePdfAsync(SelectedInvoice, path);
            await _dialogService.ShowInfoAsync("Succès", "PDF généré avec succès.");
        }
    }

    [RelayCommand]
    private async Task RecordPayment()
    {
        if (SelectedInvoice == null) return;
        if (SelectedInvoice.Status == "Payée")
        {
            await _dialogService.ShowErrorAsync("Erreur", "Cette facture est déjà payée.");
            return;
        }
        var vm = new PaymentEditViewModel(new Payment
        {
            InvoiceId = SelectedInvoice.Id,
            ClientId = SelectedInvoice.ClientId,
            Amount = SelectedInvoice.Balance,
            Date = DateTime.Now
        }, _context, _dialogService, _numberingService);
        var window = new Views.PaymentEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Payments.Add(vm.Payment);
            SelectedInvoice.PaidAmount += vm.Payment.Amount;
            SelectedInvoice.Status = SelectedInvoice.Balance <= 0 ? "Payée" : "Partiellement payée";
            _context.Invoices.Update(SelectedInvoice);

            var client = await _context.Clients.FindAsync(SelectedInvoice.ClientId);
            if (client != null)
            {
                // .AsEnumerable() : SQLite ne peut pas traduire Sum() sur un decimal en SQL directement.
                client.TotalPaid = _context.Payments.Where(p => p.ClientId == client.Id && !p.IsDeleted).AsEnumerable().Sum(p => p.Amount);
            }

            _context.CashTransactions.Add(new CashTransaction
            {
                Date = vm.Payment.Date,
                Label = $"Paiement facture {SelectedInvoice.Number}",
                Category = "Encaissement client",
                Type = "Entrée",
                Amount = vm.Payment.Amount,
                Mode = vm.Payment.Mode,
                Reference = vm.Payment.Reference,
                InvoiceId = SelectedInvoice.Id
            });

            await _context.SaveChangesAsync();
            LoadInvoices();
        }
    }

    [RelayCommand]
    private void Refresh() => LoadInvoices();
}

public partial class InvoiceEditViewModel : ObservableObject
{
    [ObservableProperty]
    private Invoice _invoice;

    [ObservableProperty]
    private ObservableCollection<InvoiceLine> _lines = new();

    [ObservableProperty]
    private ObservableCollection<Client> _clients = new();

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private decimal _lineQuantity = 1;

    [ObservableProperty]
    private decimal _lineDiscount = 0;

    [ObservableProperty]
    private ObservableCollection<string> _statuses = new() { "Brouillon", "Impayée", "Partiellement payée", "Payée", "Annulée" };

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;

    public InvoiceEditViewModel(Invoice invoice, AppDbContext context, IDialogService dialogService)
    {
        _invoice = invoice;
        _context = context;
        _dialogService = dialogService;
        Clients = new ObservableCollection<Client>(context.Clients.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToList());
        Products = new ObservableCollection<Product>(context.Products.Where(p => !p.IsDeleted && p.IsActive).OrderBy(p => p.Designation).ToList());
        if (invoice.Id > 0)
        {
            Lines = new ObservableCollection<InvoiceLine>(context.InvoiceLines.Where(l => l.InvoiceId == invoice.Id).Include(l => l.Product).ToList());
            Recalculate();
        }
    }

    [RelayCommand]
    private void AddLine()
    {
        if (SelectedProduct == null) return;
        var line = new InvoiceLine
        {
            ProductId = SelectedProduct.Id,
            ProductReference = SelectedProduct.Reference,
            Designation = SelectedProduct.Designation,
            Quantity = LineQuantity,
            Unit = SelectedProduct.Unit,
            UnitPrice = SelectedProduct.SalePrice,
            Discount = LineDiscount
        };
        Lines.Add(line);
        Recalculate();
        SelectedProduct = null;
        LineQuantity = 1;
        LineDiscount = 0;
    }

    [RelayCommand]
    private void RemoveLine(InvoiceLine line)
    {
        Lines.Remove(line);
        Recalculate();
    }

    [RelayCommand]
    private void Recalculate()
    {
        // Ici Lines est une ObservableCollection en memoire (pas IQueryable),
        // donc Sum() s'execute deja en LINQ to Objects : pas de risque SQLite.
        Invoice.SubTotal = Lines.Sum(l => l.Quantity * l.UnitPrice);
        Invoice.Discount = Lines.Sum(l => l.Discount);
        Invoice.TaxAmount = (Invoice.SubTotal - Invoice.Discount) * Invoice.TaxRate / 100;
        Invoice.Total = Invoice.SubTotal - Invoice.Discount + Invoice.TaxAmount;
    }

    [RelayCommand]
    private void Save(Window window)
    {
        if (Invoice.ClientId == 0)
        {
            _dialogService.ShowErrorAsync("Erreur", "Veuillez sélectionner un client.").Wait();
            return;
        }
        if (!Lines.Any())
        {
            _dialogService.ShowErrorAsync("Erreur", "Ajoutez au moins une ligne.").Wait();
            return;
        }
        window.DialogResult = true;
        window.Close();
    }

    [RelayCommand]
    private void Cancel(Window window)
    {
        window.DialogResult = false;
        window.Close();
    }
}
