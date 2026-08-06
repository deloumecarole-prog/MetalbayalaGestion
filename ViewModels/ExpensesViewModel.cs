using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalBayalaGestion.Data;
using MetalBayalaGestion.Models;
using MetalBayalaGestion.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace MetalBayalaGestion.ViewModels;

public partial class ExpensesViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Expense> _expenses = new();

    [ObservableProperty]
    private Expense? _selectedExpense;

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;

    public ExpensesViewModel(AppDbContext context, IDialogService dialogService)
    {
        _context = context;
        _dialogService = dialogService;
        LoadExpenses();
    }

    private void LoadExpenses()
    {
        Expenses.Clear();
        var list = _context.Expenses.Where(e => !e.IsDeleted).OrderByDescending(e => e.Date).ToList();
        foreach (var e in list) Expenses.Add(e);
    }

    [RelayCommand]
    private async Task AddExpense()
    {
        var vm = new ExpenseEditViewModel(new Expense { Date = DateTime.Now }, _context);
        var window = new Views.ExpenseEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Expenses.Add(vm.Expense);
            _context.CashTransactions.Add(new CashTransaction
            {
                Date = vm.Expense.Date,
                Label = vm.Expense.Description,
                Category = vm.Expense.Category,
                Type = "Sortie",
                Amount = vm.Expense.Amount,
                Mode = vm.Expense.Mode
            });
            await _context.SaveChangesAsync();
            LoadExpenses();
        }
    }

    [RelayCommand]
    private async Task EditExpense()
    {
        if (SelectedExpense == null) return;
        var vm = new ExpenseEditViewModel(SelectedExpense, _context);
        var window = new Views.ExpenseEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Expenses.Update(vm.Expense);
            await _context.SaveChangesAsync();
            LoadExpenses();
        }
    }

    [RelayCommand]
    private async Task DeleteExpense()
    {
        if (SelectedExpense == null) return;
        if (!await _dialogService.ShowConfirmAsync("Confirmation", "Supprimer cette dépense ?")) return;
        SelectedExpense.IsDeleted = true;
        await _context.SaveChangesAsync();
        LoadExpenses();
    }

    [RelayCommand]
    private void Refresh() => LoadExpenses();
}

public partial class ExpenseEditViewModel : ObservableObject
{
    [ObservableProperty]
    private Expense _expense;

    [ObservableProperty]
    private ObservableCollection<string> _categories = new() { "Transport", "Électricité", "Eau", "Loyer", "Salaires", "Fournitures", "Maintenance", "Autre" };

    [ObservableProperty]
    private ObservableCollection<string> _modes = new() { "Espèces", "Orange Money", "Moov Money", "Wave", "Virement bancaire", "Chèque", "Autre" };

    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    public ExpenseEditViewModel(Expense expense, AppDbContext context)
    {
        _expense = expense;
        Suppliers = new ObservableCollection<Supplier>(context.Suppliers.Where(s => !s.IsDeleted).OrderBy(s => s.Name).ToList());
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
