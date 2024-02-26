using Application.Interfaces;
using Application.Models;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
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

