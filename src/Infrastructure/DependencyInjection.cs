using Domain.Repositories;
using Infrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Data;
using System.Data.Common;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string dbConnectionString)
    {
        string connectionString = WithoutGssEncryption(dbConnectionString);
        services.AddScoped<IDbConnection>(sp => new NpgsqlConnection(connectionString));

        //inject Repository
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISellerRepository, SellerRepository>();
        services.AddScoped<ISeriesRepository, SeriesRepository>();

        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        services.AddScoped<IUserRepository, UserRepository>();

        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        return services;
    }

    /// <summary>
    /// Npgsql 10 tries GSS (Kerberos) encryption by default and logs
    /// "libgssapi_krb5.so.2: cannot open shared object file" when the image has no Kerberos library.
    /// Postgres here uses password authentication, so turn it off, unless the connection string sets it.
    /// </summary>
    internal static string WithoutGssEncryption(string connectionString)
    {
        // Npgsql accepts the keyword with and without spaces.
        DbConnectionStringBuilder given = new() { ConnectionString = connectionString };
        if (given.ContainsKey("GSS Encryption Mode") || given.ContainsKey("GssEncryptionMode"))
            return connectionString;

        return new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable }.ConnectionString;
    }
}
