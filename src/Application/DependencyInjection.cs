using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace Application;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPasswordEncryptionService, PasswordEncryptionService>();
        services.AddScoped<IInvoiceService, InvoiceService>();

        QuestPDF.Settings.License = LicenseType.Community;
    }
}

