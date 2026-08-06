using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalBayalaGestion.Data;
using MetalBayalaGestion.Models;
using MetalBayalaGestion.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MetalBayalaGestion.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime _startDate = DateTime.Now.Date;

    [ObservableProperty]
    private DateTime _endDate = DateTime.Now.Date;

    [ObservableProperty]
    private string _selectedPreset = "Aujourd'hui";

    [ObservableProperty]
    private decimal _totalSales;

    [ObservableProperty]
    private decimal _totalCashIn;

    [ObservableProperty]
    private decimal _totalReceivables;

    [ObservableProperty]
    private decimal _totalExpenses;

    [ObservableProperty]
    private decimal _cashBalance;

    [ObservableProperty]
    private ObservableCollection<StockMovement> _stockMovements = new();

    [ObservableProperty]
    private ObservableCollection<Product> _lowStockProducts = new();

    public ObservableCollection<string> Presets { get; } = new() { "Aujourd'hui", "Ce mois-ci", "Personnalisé" };

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;
    private readonly IPdfService _pdfService;

    public ReportsViewModel(AppDbContext context, IDialogService dialogService, IPdfService pdfService)
    {
        _context = context;
        _dialogService = dialogService;
        _pdfService = pdfService;
        ApplyPreset();
        GenerateReport();
    }

    partial void OnSelectedPresetChanged(string value) => ApplyPreset();

    private void ApplyPreset()
    {
        var today = DateTime.Now.Date;
        switch (SelectedPreset)
        {
            case "Aujourd'hui":
                StartDate = today;
                EndDate = today;
                break;
            case "Ce mois-ci":
                StartDate = new DateTime(today.Year, today.Month, 1);
                EndDate = today;
                break;
            case "Personnalisé":
                break;
        }
        if (SelectedPreset != "Personnalisé")
            GenerateReport();
    }

    [RelayCommand]
    private void GenerateReport()
    {
        var start = StartDate.Date;
        var end = EndDate.Date.AddDays(1).AddTicks(-1);

        // .AsEnumerable() partout ci-dessous : SQLite ne peut pas traduire Sum()
        // sur des colonnes decimal en SQL, il faut forcer le calcul en memoire.
        TotalSales = _context.Invoices
            .Where(i => i.Date >= start && i.Date <= end && !i.IsDeleted)
            .AsEnumerable()
            .Sum(i => i.Total);

        TotalCashIn = _context.Payments
            .Where(p => p.Date >= start && p.Date <= end && !p.IsDeleted)
            .AsEnumerable()
            .Sum(p => p.Amount);

        TotalReceivables = _context.Invoices
            .Where(i => !i.IsDeleted && (i.Status == "Impayée" || i.Status == "Partiellement payée"))
            .AsEnumerable()
            .Sum(i => i.Balance);

        TotalExpenses = _context.Expenses
            .Where(e => e.Date >= start && e.Date <= end && !e.IsDeleted)
            .AsEnumerable()
            .Sum(e => e.Amount);

        var totalIn = _context.CashTransactions.Where(t => t.Type == "Entrée" && !t.IsDeleted).AsEnumerable().Sum(t => t.Amount);
        var totalOut = _context.CashTransactions.Where(t => t.Type == "Sortie" && !t.IsDeleted).AsEnumerable().Sum(t => t.Amount);
        CashBalance = totalIn - totalOut;

        StockMovements.Clear();
        var movements = _context.StockMovements
            .Where(m => m.CreatedAt >= start && m.CreatedAt <= end)
            .Include(m => m.Product)
            .OrderByDescending(m => m.CreatedAt)
            .ToList();
        foreach (var m in movements) StockMovements.Add(m);

        LowStockProducts.Clear();
        // .AsEnumerable() : SQLite ne peut pas traduire ORDER BY sur un decimal en SQL directement.
        var low = _context.Products
            .Where(p => p.StockQuantity <= p.MinStock && !p.IsDeleted)
            .AsEnumerable()
            .OrderBy(p => p.StockQuantity)
            .ToList();
        foreach (var p in low) LowStockProducts.Add(p);
    }

    [RelayCommand]
    private async Task PrintReport()
    {
        var path = await _dialogService.ShowSaveFileDialogAsync("Exporter rapport PDF", "PDF|*.pdf", $"Rapport_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.pdf");
        if (path != null)
        {
            await _pdfService.GenerateReportPdfAsync(StartDate, EndDate, TotalSales, TotalCashIn, TotalReceivables, TotalExpenses, CashBalance, StockMovements.ToList(), LowStockProducts.ToList(), path);
            await _dialogService.ShowInfoAsync("Succès", "Rapport PDF généré avec succès.");
        }
    }
}
