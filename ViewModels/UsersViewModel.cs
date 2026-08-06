using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalBayalaGestion.Data;
using MetalBayalaGestion.Models;
using MetalBayalaGestion.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MetalBayalaGestion.ViewModels;

public partial class UsersViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<User> _users = new();

    [ObservableProperty]
    private User? _selectedUser;

    [ObservableProperty]
    private string _searchText = "";

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;

    public UsersViewModel(AppDbContext context, IDialogService dialogService)
    {
        _context = context;
        _dialogService = dialogService;
        LoadUsers();
    }

    partial void OnSearchTextChanged(string value) => FilterUsers();

    private void LoadUsers()
    {
        Users.Clear();
        var list = _context.Users.Where(u => !u.IsDeleted).OrderBy(u => u.FullName).ToList();
        foreach (var u in list) Users.Add(u);
    }

    private void FilterUsers()
    {
        Users.Clear();
        var query = _context.Users.Where(u => !u.IsDeleted).AsQueryable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(s) || (u.FullName != null && u.FullName.ToLower().Contains(s)));
        }
        foreach (var u in query.OrderBy(u => u.FullName).ToList()) Users.Add(u);
    }

    [RelayCommand]
    private async Task AddUser()
    {
        var vm = new UserEditViewModel(new User { IsActive = true, Role = "Caissière" }, _context);
        var window = new Views.UserEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            vm.User.PasswordHash = PasswordHasher.HashPassword(vm.PlainPassword);
            _context.Users.Add(vm.User);
            await _context.SaveChangesAsync();
            LoadUsers();
        }
    }

    [RelayCommand]
    private async Task EditUser()
    {
        if (SelectedUser == null) return;
        var vm = new UserEditViewModel(SelectedUser, _context);
        var window = new Views.UserEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            if (!string.IsNullOrWhiteSpace(vm.PlainPassword))
                vm.User.PasswordHash = PasswordHasher.HashPassword(vm.PlainPassword);
            _context.Users.Update(vm.User);
            await _context.SaveChangesAsync();
            LoadUsers();
        }
    }

    [RelayCommand]
    private async Task DeleteUser()
    {
        if (SelectedUser == null) return;
        if (SelectedUser.Username == "admin")
        {
            await _dialogService.ShowErrorAsync("Erreur", "L'utilisateur admin ne peut pas être supprimé.");
            return;
        }
        if (!await _dialogService.ShowConfirmAsync("Confirmation", $"Supprimer l'utilisateur {SelectedUser.FullName ?? SelectedUser.Username} ?")) return;
        SelectedUser.IsDeleted = true;
        await _context.SaveChangesAsync();
        LoadUsers();
    }

    [RelayCommand]
    private void Refresh() => LoadUsers();
}

public partial class UserEditViewModel : ObservableObject
{
    [ObservableProperty]
    private User _user;

    [ObservableProperty]
    private string _plainPassword = "";

    [ObservableProperty]
    private string _confirmPassword = "";

    [ObservableProperty]
    private ObservableCollection<string> _roles = new() { "Administrateur", "Gestionnaire de stock", "Caissière" };

    private readonly AppDbContext _context;

    public UserEditViewModel(User user, AppDbContext context)
    {
        _user = user;
        _context = context;
    }

    [RelayCommand]
    private void Save(Window window)
    {
        if (string.IsNullOrWhiteSpace(User.Username))
        {
            MessageBox.Show("Le nom d'utilisateur est obligatoire.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (User.Id == 0 && string.IsNullOrWhiteSpace(PlainPassword))
        {
            MessageBox.Show("Le mot de passe est obligatoire pour un nouvel utilisateur.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (!string.IsNullOrWhiteSpace(PlainPassword) && PlainPassword != ConfirmPassword)
        {
            MessageBox.Show("Les mots de passe ne correspondent pas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
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
