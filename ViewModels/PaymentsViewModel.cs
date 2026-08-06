using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalBayalaGestion.Data;
using MetalBayalaGestion.Models;
using MetalBayalaGestion.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace MetalBayalaGestion.ViewModels;

public partial class PaymentsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Payment> _payments = new();

    [ObservableProperty]
    private Payment? _selectedPayment;

    [ObservableProperty]
    private string _searchText = "";

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;
    private readonly INumberingService _numberingService;

    public PaymentsViewModel(AppDbContext context, IDialogService dialogService, INumberingService numberingService)
    {
        _context = context;
        _dialogService = dialogService;
        _numberingService = numberingService;
        LoadPayments();
    }

    partial void OnSearchTextChanged(string value) => FilterPayments();

    private void LoadPayments()
    {
        Payments.Clear();
        var list = _context.Payments.Where(p => !p.IsDeleted).Include(p => p.Client).Include(p => p.Invoice).OrderByDescending(p => p.Date).ToList();
        foreach (var p in list) Payments.Add(p);
    }

    private void FilterPayments()
    {
        Payments.Clear();
        var query = _context.Payments.Where(p => !p.IsDeleted).Include(p => p.Client).AsQueryable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.ToLower();
            query = query.Where(p => p.Number.ToLower().Contains(s) || p.Client.Name.ToLower().Contains(s));
        }
        foreach (var p in query.OrderByDescending(p => p.Date).ToList()) Payments.Add(p);
    }

    [RelayCommand]
    private async Task AddPayment()
    {
        var number = await _numberingService.GetNextPaymentNumberAsync();
        var vm = new PaymentEditViewModel(new Payment { Number = number, Date = DateTime.Now }, _context, _dialogService, _numberingService);
        var window = new Views.PaymentEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Payments.Add(vm.Payment);

            if (vm.Payment.InvoiceId.HasValue)
            {
                var inv = await _context.Invoices.FindAsync(vm.Payment.InvoiceId.Value);
                if (inv != null)
                {
                    inv.PaidAmount += vm.Payment.Amount;
                    inv.Status = inv.Balance <= 0 ? "Payée" : "Partiellement payée";
                    _context.Invoices.Update(inv);
                }
            }

            var client = await _context.Clients.FindAsync(vm.Payment.ClientId);
            if (client != null)
            {
                // .AsEnumerable() : SQLite ne peut pas traduire Sum() sur un decimal en SQL directement.
                client.TotalPaid = _context.Payments.Where(p => p.ClientId == client.Id && !p.IsDeleted).AsEnumerable().Sum(p => p.Amount);
            }

            _context.CashTransactions.Add(new CashTransaction
            {
                Date = vm.Payment.Date,
                Label = vm.Payment.InvoiceId.HasValue ? $"Paiement facture" : "Encaissement client",
                Category = "Encaissement client",
                Type = "Entrée",
                Amount = vm.Payment.Amount,
                Mode = vm.Payment.Mode,
                Reference = vm.Payment.Reference
            });

            await _context.SaveChangesAsync();
            LoadPayments();
        }
    }

    [RelayCommand]
    private async Task DeletePayment()
    {
        if (SelectedPayment == null) return;
        if (!await _dialogService.ShowConfirmAsync("Confirmation", $"Supprimer le paiement {SelectedPayment.Number} ?")) return;
        SelectedPayment.IsDeleted = true;
        await _context.SaveChangesAsync();
        LoadPayments();
    }

    [RelayCommand]
    private void Refresh() => LoadPayments();
}

public partial class PaymentEditViewModel : ObservableObject
{
    [ObservableProperty]
    private Payment _payment;

    [ObservableProperty]
    private ObservableCollection<Client> _clients = new();

    [ObservableProperty]
    private ObservableCollection<Invoice> _invoices = new();

    [ObservableProperty]
    private ObservableCollection<string> _modes = new() { "Espèces", "Orange Money", "Moov Money", "Wave", "Virement bancaire", "Chèque", "Autre" };

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;

    public PaymentEditViewModel(Payment payment, AppDbContext context, IDialogService dialogService, INumberingService numberingService)
    {
        _payment = payment;
        _context = context;
        _dialogService = dialogService;
        Clients = new ObservableCollection<Client>(context.Clients.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToList());
        LoadInvoices();
    }

    partial void OnPaymentChanged(Payment value) => LoadInvoices();

    private void LoadInvoices()
    {
        Invoices.Clear();
        var query = _context.Invoices.Where(i => !i.IsDeleted && i.Status != "Payée" && i.Status != "Annulée").AsQueryable();
        if (Payment.ClientId > 0)
            query = query.Where(i => i.ClientId == Payment.ClientId);
        foreach (var i in query.OrderByDescending(i => i.Date).ToList()) Invoices.Add(i);
    }

    [RelayCommand]
    private void Save(Window window)
    {
        if (Payment.ClientId == 0)
        {
            _dialogService.ShowErrorAsync("Erreur", "Veuillez sélectionner un client.").Wait();
            return;
        }
        if (Payment.Amount <= 0)
        {
            _dialogService.ShowErrorAsync("Erreur", "Le montant doit être supérieur à 0.").Wait();
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
