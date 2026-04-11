using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Interfaces
{
    public interface ITransferService
    {
        Task<IEnumerable<StockTransfer>> GetAllAsync();
        Task<StockTransfer?> GetByIdAsync(int id);
        Task<ServiceResult> CreateAsync(StockTransferViewModel model, string userId);
        Task<ServiceResult> ApproveAsync(int id, string userId);
        Task<ServiceResult> DispatchAsync(int id);
        Task<ServiceResult> ReceiveAsync(int id);
        Task<ServiceResult> CancelAsync(int id);
    }
}
