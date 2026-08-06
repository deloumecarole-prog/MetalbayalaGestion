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

public partial class QuotesViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Quote> _quotes = new();

    [ObservableProperty]
    private Quote? _selectedQuote;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _statusFilter = "Tous";

    // Ajoute la liste des statuts pour alimenter le ComboBox de filtre dans QuotesView.xaml
    // (ItemsSource="{Binding Statuses}"), qui n'avait pas de source avant cet ajout.
    public ObservableCollection<string> Statuses { get; } = new()
    {
        "Tous", "Brouillon", "Envoyé", "Accepté", "Refusé", "Expiré", "Converti"
    };

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;
    private readonly INumberingService _numberingService;
    private readonly IPdfService _pdfService;

    public QuotesViewModel(AppDbContext context, IDialogService dialogService, INumberingService numberingService, IPdfService pdfService)
    {
        _context = context;
        _dialogService = dialogService;
        _numberingService = numberingService;
        _pdfService = pdfService;
        LoadQuotes();
    }

    partial void OnSearchTextChanged(string value) => FilterQuotes();
    partial void OnStatusFilterChanged(string value) => FilterQuotes();

    private void LoadQuotes()
    {
        Quotes.Clear();
        var list = _context.Quotes.Where(q => !q.IsDeleted).Include(q => q.Client).OrderByDescending(q => q.Date).ToList();
        foreach (var q in list) Quotes.Add(q);
    }

    private void FilterQuotes()
    {
        Quotes.Clear();
        var query = _context.Quotes.Where(q => !q.IsDeleted).Include(q => q.Client).AsQueryable();
        if (StatusFilter != "Tous")
            query = query.Where(q => q.Status == StatusFilter);
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.ToLower();
            query = query.Where(q => q.Number.ToLower().Contains(s) || q.Client.Name.ToLower().Contains(s));
        }
        foreach (var q in query.OrderByDescending(q => q.Date).ToList()) Quotes.Add(q);
    }

    [RelayCommand]
    private async Task AddQuote()
    {
        var number = await _numberingService.GetNextQuoteNumberAsync();
        var vm = new QuoteEditViewModel(new Quote { Number = number, Date = DateTime.Now, ValidUntil = DateTime.Now.AddDays(30) }, _context, _dialogService);
        var window = new Views.QuoteEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Quotes.Add(vm.Quote);
            foreach (var line in vm.Lines)
            {
                line.Quote = vm.Quote; // navigation : EF Core resout QuoteId apres l'insertion du devis
                _context.QuoteLines.Add(line);
            }
            await _context.SaveChangesAsync();
            LoadQuotes();
        }
    }

    [RelayCommand]
    private async Task EditQuote()
    {
        if (SelectedQuote == null) return;
        var vm = new QuoteEditViewModel(SelectedQuote, _context, _dialogService);
        var window = new Views.QuoteEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Quotes.Update(vm.Quote);
            var existing = _context.QuoteLines.Where(l => l.QuoteId == vm.Quote.Id).ToList();
            _context.QuoteLines.RemoveRange(existing);
            foreach (var line in vm.Lines)
            {
                line.QuoteId = vm.Quote.Id;
                _context.QuoteLines.Add(line);
            }
            await _context.SaveChangesAsync();
            LoadQuotes();
        }
    }

    [RelayCommand]
    private async Task DeleteQuote()
    {
        if (SelectedQuote == null) return;
        if (!await _dialogService.ShowConfirmAsync("Confirmation", $"Supprimer le devis {SelectedQuote.Number} ?")) return;
        SelectedQuote.IsDeleted = true;
        await _context.SaveChangesAsync();
        LoadQuotes();
    }

    [RelayCommand]
    private async Task PrintQuote()
    {
        if (SelectedQuote == null) return;
        var path = await _dialogService.ShowSaveFileDialogAsync("Exporter PDF", "PDF|*.pdf", $"{SelectedQuote.Number}.pdf");
        if (path != null)
        {
            await _pdfService.GenerateQuotePdfAsync(SelectedQuote, path);
            await _dialogService.ShowInfoAsync("Succès", "PDF généré avec succès.");
        }
    }

    [RelayCommand]
    private async Task ConvertToInvoice()
    {
        if (SelectedQuote == null) return;
        if (SelectedQuote.Status != "Accepté")
        {
            await _dialogService.ShowErrorAsync("Erreur", "Le devis doit être accepté pour être converti en facture.");
            return;
        }
        if (!await _dialogService.ShowConfirmAsync("Confirmation", "Convertir ce devis en facture ?")) return;

        // Vérification stock
        var quoteLines = _context.QuoteLines.Where(l => l.QuoteId == SelectedQuote.Id).ToList();
        foreach (var line in quoteLines)
        {
            var product = await _context.Products.FindAsync(line.ProductId);
            if (product != null && product.StockQuantity < line.Quantity)
            {
                if (!await _dialogService.ShowConfirmAsync("Stock insuffisant",
                    $"Le produit '{product.Designation}' n'a que {product.StockQuantity:0.##} {product.Unit} en stock (demandé : {line.Quantity:0.##}). Continuer quand même ?"))
                    return;
            }
        }

        var number = await _numberingService.GetNextInvoiceNumberAsync();
        var invoice = new Invoice
        {
            Number = number,
            Date = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            ClientId = SelectedQuote.ClientId,
            ClientAddress = SelectedQuote.ClientAddress,
            ClientPhone = SelectedQuote.ClientPhone,
            Status = "Impayée",
            SubTotal = SelectedQuote.SubTotal,
            Discount = SelectedQuote.Discount,
            TaxRate = SelectedQuote.TaxRate,
            TaxAmount = SelectedQuote.TaxAmount,
            Total = SelectedQuote.Total,
            QuoteId = SelectedQuote.Id,
            Notes = $"Créé depuis le devis {SelectedQuote.Number}"
        };
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        foreach (var line in quoteLines)
        {
            _context.InvoiceLines.Add(new InvoiceLine
            {
                InvoiceId = invoice.Id,
                ProductId = line.ProductId,
                ProductReference = line.ProductReference,
                Designation = line.Designation,
                Quantity = line.Quantity,
                Unit = line.Unit,
                UnitPrice = line.UnitPrice,
                Discount = line.Discount
            });

            // Décrémentation stock
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
                    Reference = invoice.Number,
                    Notes = $"Vente - Facture {invoice.Number} (devis {SelectedQuote.Number})"
                });
            }
        }

        SelectedQuote.Status = "Converti";
        SelectedQuote.ConvertedTo = invoice.Number;
        await _context.SaveChangesAsync();
        LoadQuotes();
        await _dialogService.ShowInfoAsync("Succès", $"Facture {number} créée avec succès.");
    }

    [RelayCommand]
    private void Refresh() => LoadQuotes();
}

public partial class QuoteEditViewModel : ObservableObject
{
    [ObservableProperty]
    private Quote _quote;

    [ObservableProperty]
    private ObservableCollection<QuoteLine> _lines = new();

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
    private ObservableCollection<string> _statuses = new() { "Brouillon", "Envoyé", "Accepté", "Refusé", "Expiré", "Converti" };

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;

    public QuoteEditViewModel(Quote quote, AppDbContext context, IDialogService dialogService)
    {
        _quote = quote;
        _context = context;
        _dialogService = dialogService;
        Clients = new ObservableCollection<Client>(context.Clients.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToList());
        Products = new ObservableCollection<Product>(context.Products.Where(p => !p.IsDeleted && p.IsActive).OrderBy(p => p.Designation).ToList());
        if (quote.Id > 0)
        {
            Lines = new ObservableCollection<QuoteLine>(context.QuoteLines.Where(l => l.QuoteId == quote.Id).Include(l => l.Product).ToList());
            Recalculate();
        }
    }

    partial void OnQuoteChanged(Quote value) => Recalculate();

    [RelayCommand]
    private void AddLine()
    {
        if (SelectedProduct == null) return;
        var line = new QuoteLine
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
    private void RemoveLine(QuoteLine line)
    {
        Lines.Remove(line);
        Recalculate();
    }

    [RelayCommand]
    private void Recalculate()
    {
        Quote.SubTotal = Lines.Sum(l => l.Quantity * l.UnitPrice);
        Quote.Discount = Lines.Sum(l => l.Discount);
        Quote.TaxAmount = (Quote.SubTotal - Quote.Discount) * Quote.TaxRate / 100;
        Quote.Total = Quote.SubTotal - Quote.Discount + Quote.TaxAmount;
    }

    [RelayCommand]
    private void Save(Window window)
    {
        if (Quote.ClientId == 0)
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
