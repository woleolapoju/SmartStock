using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Interfaces
{
    public interface IPurchaseService
    {
        Task<IEnumerable<Purchase>> GetAllAsync();
        Task<Purchase?> GetByIdAsync(int id);
        Task<ServiceResult> CreateAsync(CreatePurchaseViewModel model, string userId);
        Task<ServiceResult> ReceiveAsync(int id);
        Task<ServiceResult> CancelAsync(int id);
    }
}
