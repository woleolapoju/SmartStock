using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Interfaces;
using SmartStock.Models;

namespace SmartStock.Services
{
    public class SystemParameterService : ISystemParameterService
    {
        private readonly ApplicationDbContext _db;

        public SystemParameterService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<SystemParameter> GetAsync()
        {
            return await _db.SystemParameters.FirstOrDefaultAsync()
                   ?? new SystemParameter();
        }

        public async Task SaveAsync(SystemParameter param, string updatedByUserId)
        {
            var existing = await _db.SystemParameters.FirstOrDefaultAsync();
            if (existing == null)
            {
                param.Id = 1;
                param.UpdatedAt = DateTime.UtcNow;
                param.UpdatedByUserId = updatedByUserId;
                _db.SystemParameters.Add(param);
            }
            else
            {
                existing.OwnerName = param.OwnerName;
                existing.TaxRate = param.TaxRate;
                existing.CurrencySymbol = param.CurrencySymbol;
                existing.CurrencyCode = param.CurrencyCode;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = updatedByUserId;
            }
            await _db.SaveChangesAsync();
        }
    }
}
