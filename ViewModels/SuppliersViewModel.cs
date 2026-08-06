using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalBayalaGestion.Data;
using MetalBayalaGestion.Models;
using MetalBayalaGestion.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace MetalBayalaGestion.ViewModels;

public partial class SuppliersViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private string _searchText = "";

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;

    public SuppliersViewModel(AppDbContext context, IDialogService dialogService)
    {
        _context = context;
        _dialogService = dialogService;
        LoadSuppliers();
    }

    partial void OnSearchTextChanged(string value) => FilterSuppliers();

    private void LoadSuppliers()
    {
        Suppliers.Clear();
        foreach (var s in _context.Suppliers.Where(s => !s.IsDeleted).OrderBy(s => s.Name).ToList())
            Suppliers.Add(s);
    }

    private void FilterSuppliers()
    {
        Suppliers.Clear();
        var query = _context.Suppliers.Where(s => !s.IsDeleted).AsQueryable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(s) || x.Code.ToLower().Contains(s));
        }
        foreach (var x in query.OrderBy(x => x.Name).ToList()) Suppliers.Add(x);
    }

    [RelayCommand]
    private async Task AddSupplier()
    {
        var vm = new SupplierEditViewModel(new Supplier { Code = $"FOU-{_context.Suppliers.Count() + 1:D3}" }, _context);
        var window = new Views.SupplierEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Suppliers.Add(vm.Supplier);
            await _context.SaveChangesAsync();
            LoadSuppliers();
        }
    }

    [RelayCommand]
    private async Task EditSupplier()
    {
        if (SelectedSupplier == null) return;
        var vm = new SupplierEditViewModel(SelectedSupplier, _context);
        var window = new Views.SupplierEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Suppliers.Update(vm.Supplier);
            await _context.SaveChangesAsync();
            LoadSuppliers();
        }
    }

    [RelayCommand]
    private async Task DeleteSupplier()
    {
        if (SelectedSupplier == null) return;
        if (!await _dialogService.ShowConfirmAsync("Confirmation", $"Supprimer le fournisseur {SelectedSupplier.Name} ?")) return;
        SelectedSupplier.IsDeleted = true;
        await _context.SaveChangesAsync();
        LoadSuppliers();
    }
}

public partial class SupplierEditViewModel : ObservableObject
{
    [ObservableProperty]
    private Supplier _supplier;

    public SupplierEditViewModel(Supplier supplier, AppDbContext context)
    {
        _supplier = supplier;
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
