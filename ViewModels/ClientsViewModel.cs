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

public partial class ClientsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Client> _clients = new();

    [ObservableProperty]
    private Client? _selectedClient;

    [ObservableProperty]
    private string _searchText = "";

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;

    public ClientsViewModel(AppDbContext context, IDialogService dialogService)
    {
        _context = context;
        _dialogService = dialogService;
        LoadClients();
    }

    partial void OnSearchTextChanged(string value) => FilterClients();

    private void LoadClients()
    {
        Clients.Clear();
        var list = _context.Clients.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToList();
        foreach (var c in list) Clients.Add(c);
    }

    private void FilterClients()
    {
        Clients.Clear();
        var query = _context.Clients.Where(c => !c.IsDeleted).AsQueryable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(s) || c.Code.ToLower().Contains(s) || (c.Phone != null && c.Phone.Contains(s)));
        }
        foreach (var c in query.OrderBy(c => c.Name).ToList()) Clients.Add(c);
    }

    // Genere le prochain code client a partir du plus grand numero deja
    // utilise (tous clients confondus, supprimes inclus, pour ne jamais
    // reutiliser un code deja pris). Un simple Count()+1 provoquait des
    // collisions "UNIQUE constraint failed" des qu'il y avait un ecart
    // entre le nombre de lignes et les codes reellement attribues
    // (donnees de demo, suppressions, etc.).
    private string GenerateNextClientCode()
    {
        var maxNumber = _context.Clients
            .Select(c => c.Code)
            .AsEnumerable()
            .Select(code =>
            {
                if (string.IsNullOrEmpty(code) || !code.StartsWith("CLI-")) return 0;
                return int.TryParse(code.Substring(4), out var n) ? n : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return $"CLI-{maxNumber + 1:D3}";
    }

    [RelayCommand]
    private async Task AddClient()
    {
        var vm = new ClientEditViewModel(new Client { Code = GenerateNextClientCode() }, _context);
        var window = new Views.ClientEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Clients.Add(vm.Client);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // Filet de securite en cas de collision malgre tout (ex. saisie manuelle
                // d'un code deja pris) : on regenere un code et on retente une fois.
                _context.Entry(vm.Client).State = EntityState.Detached;
                vm.Client.Code = GenerateNextClientCode();
                _context.Clients.Add(vm.Client);
                await _context.SaveChangesAsync();
            }
            LoadClients();
        }
    }

    [RelayCommand]
    private async Task EditClient()
    {
        if (SelectedClient == null) return;
        var vm = new ClientEditViewModel(SelectedClient, _context);
        var window = new Views.ClientEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Clients.Update(vm.Client);
            await _context.SaveChangesAsync();
            LoadClients();
        }
    }

    [RelayCommand]
    private async Task DeleteClient()
    {
        if (SelectedClient == null) return;
        if (!await _dialogService.ShowConfirmAsync("Confirmation", $"Supprimer le client {SelectedClient.Name} ?")) return;
        SelectedClient.IsDeleted = true;
        await _context.SaveChangesAsync();
        LoadClients();
    }

    [RelayCommand]
    private void Refresh() => LoadClients();
}

public partial class ClientEditViewModel : ObservableObject
{
    [ObservableProperty]
    private Client _client;

    public ClientEditViewModel(Client client, AppDbContext context)
    {
        _client = client;
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
