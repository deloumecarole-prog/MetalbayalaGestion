using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalBayalaGestion.Data;
using MetalBayalaGestion.Models;
using MetalBayalaGestion.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace MetalBayalaGestion.ViewModels;

public partial class CategoriesViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Category? _selectedCategory;

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;

    public CategoriesViewModel(AppDbContext context, IDialogService dialogService)
    {
        _context = context;
        _dialogService = dialogService;
        LoadCategories();
    }

    private void LoadCategories()
    {
        Categories.Clear();
        foreach (var c in _context.Categories.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToList())
            Categories.Add(c);
    }

    [RelayCommand]
    private async Task AddCategory()
    {
        var vm = new CategoryEditViewModel(new Category(), _context);
        var window = new Views.CategoryEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Categories.Add(vm.Category);
            await _context.SaveChangesAsync();
            LoadCategories();
        }
    }

    [RelayCommand]
    private async Task EditCategory()
    {
        if (SelectedCategory == null) return;
        var vm = new CategoryEditViewModel(SelectedCategory, _context);
        var window = new Views.CategoryEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.Categories.Update(vm.Category);
            await _context.SaveChangesAsync();
            LoadCategories();
        }
    }

    [RelayCommand]
    private async Task DeleteCategory()
    {
        if (SelectedCategory == null) return;
        if (!await _dialogService.ShowConfirmAsync("Confirmation", $"Supprimer la catégorie {SelectedCategory.Name} ?")) return;
        SelectedCategory.IsDeleted = true;
        await _context.SaveChangesAsync();
        LoadCategories();
    }
}

public partial class CategoryEditViewModel : ObservableObject
{
    [ObservableProperty]
    private Category _category;

    public CategoryEditViewModel(Category category, AppDbContext context)
    {
        _category = category;
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
