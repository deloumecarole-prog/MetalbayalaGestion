using CommunityToolkit.Mvvm.ComponentModel;
using MetalBayalaGestion.Data;
using MetalBayalaGestion.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;

namespace MetalBayalaGestion.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal _todaySales;
    [ObservableProperty]
    private decimal _monthSales;
    [ObservableProperty]
    private int _clientCount;
    [ObservableProperty]
    private int _productCount;
    [ObservableProperty]
    private int _pendingQuotes;
    [ObservableProperty]
    private int _unpaidInvoices;
    [ObservableProperty]
    private decimal _totalReceivables;
    [ObservableProperty]
    private decimal _totalPayments;
    [ObservableProperty]
    private int _lowStockCount;

    public ObservableCollection<Invoice> RecentInvoices { get; } = new();
    public ObservableCollection<Quote> RecentQuotes { get; } = new();
    public ObservableCollection<Payment> RecentPayments { get; } = new();
    public ObservableCollection<Product> LowStockProducts { get; } = new();

    private readonly AppDbContext _context;

    public DashboardViewModel(AppDbContext context)
    {
        _context = context;
        LoadData();
    }

    private void LoadData()
    {
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        // .AsEnumerable() force le calcul en memoire (LINQ to Objects) au lieu
        // de laisser EF Core traduire Sum() en SQL, ce que SQLite ne supporte
        // pas nativement pour le type decimal.
        TodaySales = _context.Payments.Where(p => p.Date.Date == today && !p.IsDeleted).AsEnumerable().Sum(p => p.Amount);
        MonthSales = _context.Payments.Where(p => p.Date >= monthStart && !p.IsDeleted).AsEnumerable().Sum(p => p.Amount);
        ClientCount = _context.Clients.Count(c => !c.IsDeleted);
        ProductCount = _context.Products.Count(p => !p.IsDeleted);
        PendingQuotes = _context.Quotes.Count(q => q.Status == "Brouillon" || q.Status == "Envoyé" && !q.IsDeleted);
        UnpaidInvoices = _context.Invoices.Count(i => (i.Status == "Impayée" || i.Status == "Partiellement payée") && !i.IsDeleted);
        TotalReceivables = _context.Invoices.Where(i => !i.IsDeleted).AsEnumerable().Sum(i => i.Balance);
        TotalPayments = _context.Payments.Where(p => !p.IsDeleted).AsEnumerable().Sum(p => p.Amount);
        LowStockCount = _context.Products.Count(p => p.StockQuantity <= p.MinStock && !p.IsDeleted);

        var low = _context.Products.Where(p => p.StockQuantity <= p.MinStock && !p.IsDeleted).Take(5).ToList();
        foreach (var p in low) LowStockProducts.Add(p);

        var recentInv = _context.Invoices.Where(i => !i.IsDeleted).OrderByDescending(i => i.Date).Take(5).Include(i => i.Client).ToList();
        foreach (var i in recentInv) RecentInvoices.Add(i);

        var recentQuot = _context.Quotes.Where(q => !q.IsDeleted).OrderByDescending(q => q.Date).Take(5).Include(q => q.Client).ToList();
        foreach (var q in recentQuot) RecentQuotes.Add(q);

        var recentPay = _context.Payments.Where(p => !p.IsDeleted).OrderByDescending(p => p.Date).Take(5).Include(p => p.Client).ToList();
        foreach (var p in recentPay) RecentPayments.Add(p);
    }
}
