using Application.Helpers.NumberToWords;
using Application.Helpers.PriceToWords;
using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISellerService, SellerService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPasswordEncryptionService, PasswordEncryptionService>();

        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<INumberToWords, NumberToWordsLT>();
        services.AddScoped<IPriceToWords, PriceToWordsLT>();
    }
}

