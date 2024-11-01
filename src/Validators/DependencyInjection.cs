using Application.Models;
using Contracts.Requests.Customer;
using Contracts.Requests.Invoice;
using Contracts.Requests.Item;
using Contracts.Requests.Seller;
using Contracts.Requests.User;
using Domain.Repositories;
using FluentValidation;
using Infrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using System.Data;
using Validators.Customer;
using Validators.Invoice;
using Validators.Item;
using Validators.Seller;
using Validators.User;

namespace Validators;

public static class DependencyInjection
{
    public static IServiceCollection AddValidations(this IServiceCollection services)
    {
        services.AddScoped<IValidator<UserAddRequest>, UserAddValidator>();
        services.AddScoped<IValidator<UserUpdateRequest>, UserUpdateValidator>();
        services.AddScoped<IValidator<UserLoginRequest>, UserLoginValidator>();

        services.AddScoped<IValidator<SellerAddRequest>, SellerAddValidator>();
        services.AddScoped<IValidator<SellerUpdateRequest>, SellerUpdateValidator>();
        services.AddScoped<IValidator<SellerModel>, SellerValidator>();

        services.AddScoped<IValidator<CustomerAddRequest>, CustomerAddValidator>();
        services.AddScoped<IValidator<CustomerUpdateRequest>, CustomerUpdateValidator>();
        services.AddScoped<IValidator<CustomerModel>, CustomerValidator>();

        services.AddScoped<IValidator<ItemAddRequest>, ItemAddValidator>();
        services.AddScoped<IValidator<ItemUpdateRequest>, ItemUpdateValidator>();
        services.AddScoped<IValidator<ItemModel>, ItemValidator>();

        services.AddScoped<IValidator<ItemAddRequest>, ItemAddValidator>();
        services.AddScoped<IValidator<ItemUpdateRequest>, ItemUpdateValidator>();

        services.AddScoped<IValidator<InvoiceAddRequest>, InvoiceAddValidator>();
        services.AddScoped<IValidator<InvoiceUpdateRequest>, InvoiceUpdateValidator>();
        services.AddScoped<IValidator<InvoiceGenerateRequest>, InvoiceGenerateValidator>();
        services.AddScoped<IValidator<InvoiceItemRequest>, InvoiceItemValidator>();
        services.AddScoped<IValidator<InvoiceItemUpdateRequest>, InvoiceItemUpdateValidator>();

        return services;
    }
}
