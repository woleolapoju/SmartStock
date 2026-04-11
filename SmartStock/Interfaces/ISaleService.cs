using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Interfaces
{
    public interface ISaleService
    {
        Task<IEnumerable<Sale>> GetAllAsync(int? storeId = null, DateTime? from = null, DateTime? to = null);
        Task<Sale?> GetByIdAsync(int id);
        Task<ServiceResult<Sale>> CreateAsync(CreateSaleViewModel model, string cashierId);
        Task<ServiceResult> VoidAsync(int id);
        Task<IEnumerable<Sale>> GetRecentAsync(int count = 10);
    }
}
