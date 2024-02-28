using Application.Interfaces;
using Application.Services;
using Domain.IOptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Application;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IInvoiceItemService, InvoiceItemService>();
        services.AddScoped<IInvoiceClientService, InvoiceClientService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPasswordEncryptionService, PasswordEncryptionService>();

    }
}

