using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartStock.Interfaces;

namespace SmartStock.Filters
{
    public class SystemParameterFilter : IAsyncActionFilter
    {
        private readonly ISystemParameterService _svc;

        public SystemParameterFilter(ISystemParameterService svc)
        {
            _svc = svc;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.Controller is Controller controller)
            {
                var param = await _svc.GetAsync();
                controller.ViewBag.CurrencySymbol = param.CurrencySymbol;
                controller.ViewBag.CurrencyCode   = param.CurrencyCode;
            }
            await next();
        }
    }
}
