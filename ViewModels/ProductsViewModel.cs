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

public partial class ProductsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private int _selectedCategoryId;

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;

    public ProductsViewModel(AppDbContext context, IDialogService dialogService)
    {
        _context = context;
        _dialogService = dialogService;
        LoadData();
    }

    partial void OnSearchTextChanged(string value) => FilterProducts();
    partial void OnSelectedCategoryIdChanged(int value) => FilterProducts();

    private void LoadData()
    {
        Categories.Clear();
        Categories.Add(new Category { Id = 0, Name = "Toutes" });
        foreach (var c in _context.Categories.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToList())
            Categories.Add(c);

        FilterProducts();
    }

    private void FilterProducts()
    {
        Products.Clear();
        var query = _context.Products.Where(p => !p.IsDeleted).Include(p => p.Category).AsQueryable();
        if (SelectedCategoryId > 0)
            query = query.Where(p => p.CategoryId == SelectedCategoryId);
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.ToLower();
            query = query.Where(p => p.Designation.ToLower().Contains(s) || p.Reference.ToLower().Contains(s) || p.Code.ToLower().Contains(s));
        }
        foreach (var p in query.OrderBy(p => p.Designation).ToList()) Products.Add(p);
    }

    [RelayCommand]
    private async Task AddProduct()
    {
        var vm = new ProductEditViewModel(new Product { Reference = $"REF-{_context.Products.Count() + 1:D3}", Code = $"P{_context.Products.Count() + 1:D3}" }, _context);
        var window = new Views.ProductEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Products.Add(vm.Product);
            await _context.SaveChangesAsync();
            FilterProducts();
        }
    }

    [RelayCommand]
    private async Task EditProduct()
    {
        if (SelectedProduct == null) return;
        var vm = new ProductEditViewModel(SelectedProduct, _context);
        var window = new Views.ProductEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Products.Update(vm.Product);
            await _context.SaveChangesAsync();
            FilterProducts();
        }
    }

    [RelayCommand]
    private async Task DeleteProduct()
    {
        if (SelectedProduct == null) return;
        if (!await _dialogService.ShowConfirmAsync("Confirmation", $"Supprimer le produit {SelectedProduct.Designation} ?")) return;
        SelectedProduct.IsDeleted = true;
        await _context.SaveChangesAsync();
        FilterProducts();
    }

    [RelayCommand]
    private void Refresh() => LoadData();
}

public partial class ProductEditViewModel : ObservableObject
{
    [ObservableProperty]
    private Product _product;

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    public ProductEditViewModel(Product product, AppDbContext context)
    {
        _product = product;
        Categories = new ObservableCollection<Category>(context.Categories.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToList());
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
