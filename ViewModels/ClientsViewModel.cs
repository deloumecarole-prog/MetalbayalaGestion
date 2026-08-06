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

    [RelayCommand]
    private async Task AddClient()
    {
        var vm = new ClientEditViewModel(new Client { Code = $"CLI-{_context.Clients.Count() + 1:D3}" }, _context);
        var window = new Views.ClientEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Clients.Add(vm.Client);
            await _context.SaveChangesAsync();
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
