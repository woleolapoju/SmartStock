using SmartStock.Models;

namespace SmartStock.Interfaces
{
    public interface ISystemParameterService
    {
        Task<SystemParameter> GetAsync();
        Task SaveAsync(SystemParameter param, string updatedByUserId);
    }
}
