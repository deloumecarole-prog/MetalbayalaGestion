using MetalBayalaGestion.Models;

namespace MetalBayalaGestion.Services;

public class CurrentUserService : ICurrentUserService
{
    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null && CurrentUser.IsActive;
    public bool IsAdminOrStockManager => IsAuthenticated && (CurrentUser!.Role == "Administrateur" || CurrentUser.Role == "Gestionnaire de stock");
    public bool IsCashier => IsAuthenticated && CurrentUser!.Role == "Caissière";

    public void SetUser(User user) => CurrentUser = user;
    public void ClearUser() => CurrentUser = null;
}
