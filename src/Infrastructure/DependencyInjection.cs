using Domain.Repositories;
using Infrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Data;

namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, string dbConnectionString)
    {
        services.AddScoped<IDbConnection>(sp => new NpgsqlConnection(dbConnectionString));

        //inject Repository
        services.AddScoped<IInvoiceItemRepository, InvoiceItemRepository>();
        services.AddScoped<IInvoiceClientRepository, InvoiceClientRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }
}