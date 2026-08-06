using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalBayalaGestion.Data;
using MetalBayalaGestion.Services;
using MetalBayalaGestion.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace MetalBayalaGestion.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _currentTitle = "Tableau de bord";

    [ObservableProperty]
    private bool _isMenuOpen = true;

    [ObservableProperty]
    private string _currentUserDisplay = "";

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;
    private readonly INumberingService _numberingService;
    private readonly IPdfService _pdfService;
    private readonly IBackupService _backupService;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<MenuItem> MenuItems { get; } = new();

    public MainViewModel(AppDbContext context, IDialogService dialogService, INumberingService numberingService, IPdfService pdfService, IBackupService backupService, ICurrentUserService currentUserService)
    {
        _context = context;
        _dialogService = dialogService;
        _numberingService = numberingService;
        _pdfService = pdfService;
        _backupService = backupService;
        _currentUserService = currentUserService;

        var user = _currentUserService.CurrentUser;
        CurrentUserDisplay = user != null ? $"{user.FullName ?? user.Username} ({user.Role})" : "Non connecté";

        // Menu commun à tous
        MenuItems.Add(new MenuItem { Title = "Tableau de bord", Icon = "ViewDashboard", ViewType = typeof(DashboardView) });
        MenuItems.Add(new MenuItem { Title = "Devis", Icon = "FileDocumentOutline", ViewType = typeof(QuotesView) });
        MenuItems.Add(new MenuItem { Title = "Factures", Icon = "ReceiptTextOutline", ViewType = typeof(InvoicesView) });
        MenuItems.Add(new MenuItem { Title = "Paiements", Icon = "CashMultiple", ViewType = typeof(PaymentsView) });
        MenuItems.Add(new MenuItem { Title = "Clients", Icon = "AccountGroupOutline", ViewType = typeof(ClientsView) });
        MenuItems.Add(new MenuItem { Title = "Caisse", Icon = "CashRegister", ViewType = typeof(CashView) });
        MenuItems.Add(new MenuItem { Title = "Dépenses", Icon = "CashMinus", ViewType = typeof(ExpensesView) });
        MenuItems.Add(new MenuItem { Title = "Bons de sortie", Icon = "TruckDelivery", ViewType = typeof(DeliveryNotesView) });
        MenuItems.Add(new MenuItem { Title = "Rapports", Icon = "ChartBar", ViewType = typeof(ReportsView) });

        // Menu réservé Administrateur / Gestionnaire de stock
        if (_currentUserService.IsAdminOrStockManager)
        {
            MenuItems.Add(new MenuItem { Title = "Produits", Icon = "PackageVariantClosed", ViewType = typeof(ProductsView) });
            MenuItems.Add(new MenuItem { Title = "Catégories", Icon = "ShapeOutline", ViewType = typeof(CategoriesView) });
            MenuItems.Add(new MenuItem { Title = "Fournisseurs", Icon = "TruckDeliveryOutline", ViewType = typeof(SuppliersView) });
            MenuItems.Add(new MenuItem { Title = "Utilisateurs", Icon = "AccountCogOutline", ViewType = typeof(UsersView) });
            MenuItems.Add(new MenuItem { Title = "Paramètres", Icon = "CogOutline", ViewType = typeof(SettingsView) });
        }

        NavigateTo("Tableau de bord");
    }

    [RelayCommand]
    private void NavigateTo(string title)
    {
        var item = MenuItems.FirstOrDefault(m => m.Title == title);
        if (item == null) return;
        CurrentTitle = title;
        CurrentView = title switch
        {
            "Tableau de bord" => new DashboardView { DataContext = new DashboardViewModel(_context) },
            "Devis" => new QuotesView { DataContext = new QuotesViewModel(_context, _dialogService, _numberingService, _pdfService) },
            "Factures" => new InvoicesView { DataContext = new InvoicesViewModel(_context, _dialogService, _numberingService, _pdfService, _currentUserService) },
            "Paiements" => new PaymentsView { DataContext = new PaymentsViewModel(_context, _dialogService, _numberingService) },
            "Clients" => new ClientsView { DataContext = new ClientsViewModel(_context, _dialogService) },
            "Produits" => new ProductsView { DataContext = new ProductsViewModel(_context, _dialogService) },
            "Catégories" => new CategoriesView { DataContext = new CategoriesViewModel(_context, _dialogService) },
            "Caisse" => new CashView { DataContext = new CashViewModel(_context, _dialogService) },
            "Dépenses" => new ExpensesView { DataContext = new ExpensesViewModel(_context, _dialogService) },
            "Fournisseurs" => new SuppliersView { DataContext = new SuppliersViewModel(_context, _dialogService) },
            "Bons de sortie" => new DeliveryNotesView { DataContext = new DeliveryNotesViewModel(_context, _dialogService, _numberingService, _pdfService) },
            "Rapports" => new ReportsView { DataContext = new ReportsViewModel(_context, _dialogService, _pdfService) },
            "Utilisateurs" => new UsersView { DataContext = new UsersViewModel(_context, _dialogService) },
            "Paramètres" => new SettingsView { DataContext = new SettingsViewModel(_context, _dialogService, _backupService) },
            _ => CurrentView
        };
    }

    [RelayCommand]
    private void ToggleMenu() => IsMenuOpen = !IsMenuOpen;

    [RelayCommand]
    private void Logout()
    {
        _currentUserService.ClearUser();
        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? System.Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
            System.Diagnostics.Process.Start(exePath);
        Application.Current.Shutdown();
    }

    // Filet de securite : quand une sauvegarde echoue quelque part (ex: contrainte
    // de cle etrangere), les entites en erreur restent "coincees" dans le suivi
    // de l'AppDbContext (partage pour toute la session) et referont echouer les
    // PROCHAINES sauvegardes sur d'autres ecrans, meme sans rapport. Cette methode
    // est appelee depuis le gestionnaire d'erreurs global (App.xaml.cs) pour vider
    // ce suivi apres chaque erreur et eviter la propagation en cascade.
    public void ClearChangeTrackerAfterError()
    {
        try { _context.ChangeTracker.Clear(); } catch { }
    }
}

public class MenuItem
{
    public string Title { get; set; } = "";
    public string Icon { get; set; } = "";
    public System.Type ViewType { get; set; } = typeof(object);
}
