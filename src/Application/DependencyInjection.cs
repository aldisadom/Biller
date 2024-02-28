using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IInvoiceItemService, InvoiceItemService>();
        services.AddScoped<IInvoiceClientService, InvoiceClientService>();
        services.AddScoped<IUserService, UserService>();
    }
}

