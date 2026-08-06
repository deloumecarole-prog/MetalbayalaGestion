using MetalBayalaGestion.Models;

namespace MetalBayalaGestion.Services;

public interface ICurrentUserService
{
    User? CurrentUser { get; }
    bool IsAuthenticated { get; }
    bool IsAdminOrStockManager { get; }
    bool IsCashier { get; }
    void SetUser(User user);
    void ClearUser();
}
