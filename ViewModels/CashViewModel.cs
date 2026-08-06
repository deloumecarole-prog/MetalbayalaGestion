using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalBayalaGestion.Data;
using MetalBayalaGestion.Models;
using MetalBayalaGestion.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace MetalBayalaGestion.ViewModels;

public partial class CashViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<CashTransaction> _transactions = new();

    [ObservableProperty]
    private decimal _currentBalance;

    [ObservableProperty]
    private decimal _totalIn;

    [ObservableProperty]
    private decimal _totalOut;

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;

    public CashViewModel(AppDbContext context, IDialogService dialogService)
    {
        _context = context;
        _dialogService = dialogService;
        LoadTransactions();
    }

    private void LoadTransactions()
    {
        Transactions.Clear();
        var list = _context.CashTransactions.Where(t => !t.IsDeleted).OrderByDescending(t => t.Date).ToList();
        foreach (var t in list) Transactions.Add(t);
        TotalIn = list.Where(t => t.Type == "Entrée").Sum(t => t.Amount);
        TotalOut = list.Where(t => t.Type == "Sortie").Sum(t => t.Amount);
        CurrentBalance = TotalIn - TotalOut;
    }

    [RelayCommand]
    private async Task AddTransaction()
    {
        var vm = new CashTransactionEditViewModel(new CashTransaction { Date = DateTime.Now }, _context);
        var window = new Views.CashTransactionEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.CashTransactions.Add(vm.Transaction);
            await _context.SaveChangesAsync();
            LoadTransactions();
        }
    }

    [RelayCommand]
    private async Task DeleteTransaction(CashTransaction transaction)
    {
        if (transaction == null) return;
        if (!await _dialogService.ShowConfirmAsync("Confirmation", "Supprimer cette transaction ?")) return;
        transaction.IsDeleted = true;
        await _context.SaveChangesAsync();
        LoadTransactions();
    }

    [RelayCommand]
    private void Refresh() => LoadTransactions();
}

public partial class CashTransactionEditViewModel : ObservableObject
{
    [ObservableProperty]
    private CashTransaction _transaction;

    [ObservableProperty]
    private ObservableCollection<string> _types = new() { "Entrée", "Sortie" };

    [ObservableProperty]
    private ObservableCollection<string> _modes = new() { "Espèces", "Orange Money", "Moov Money", "Wave", "Virement bancaire", "Chèque", "Autre" };

    [ObservableProperty]
    private ObservableCollection<string> _categories = new() { "Encaissement client", "Dépense", "Autre entrée", "Autre sortie" };

    public CashTransactionEditViewModel(CashTransaction transaction, AppDbContext context)
    {
        _transaction = transaction;
    }

    [RelayCommand]
    private void Save(Window window)
    {
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
