using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalBayalaGestion.Data;
using MetalBayalaGestion.Models;
using MetalBayalaGestion.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Windows;

namespace MetalBayalaGestion.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public LoginViewModel(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [RelayCommand]
    private async Task LoginAsync(Window? window)
    {
        if (window == null) return;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Veuillez saisir un nom d'utilisateur et un mot de passe.";
            return;
        }

        IsLoading = true;
        ErrorMessage = "";

        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == Username && u.IsActive);
            if (user == null || !PasswordHasher.VerifyPassword(Password, user.PasswordHash))
            {
                ErrorMessage = "Nom d'utilisateur ou mot de passe incorrect.";
                IsLoading = false;
                return;
            }

            _currentUserService.SetUser(user);
            window.DialogResult = true;
            window.Close();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel(Window? window)
    {
        window?.Close();
        Application.Current.Shutdown();
    }
}
