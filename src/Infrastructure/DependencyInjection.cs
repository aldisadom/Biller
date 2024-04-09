using Domain.Repositories;
using Infrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using System.Data;

namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, string dbConnectionString)
    {
        services.AddScoped<IDbConnection>(sp => new NpgsqlConnection(dbConnectionString));

        //inject Repository
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISellerRepository, SellerRepository>();

        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        services.AddScoped<IUserRepository, UserRepository>();

        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        QuestPDF.Settings.License = LicenseType.Community;

        FontManager.RegisterFontWithCustomName("calibri", File.OpenRead("/Fonts/calibri.ttf"));
    }
}
