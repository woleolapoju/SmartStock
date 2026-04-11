using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllAsync(string? search = null, int? categoryId = null, bool activeOnly = true);
        Task<Product?> GetByIdAsync(int id);
        Task<Product?> GetBySkuAsync(string sku);
        Task<ServiceResult> CreateAsync(ProductViewModel model);
        Task<ServiceResult> UpdateAsync(int id, ProductViewModel model);
        Task<ServiceResult> DeleteAsync(int id);
        Task<IEnumerable<Category>> GetCategoriesAsync();
    }
}
