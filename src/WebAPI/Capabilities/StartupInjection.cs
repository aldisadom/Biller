using Application;
using Clients;
using Domain.IOptions;
using Infrastructure;
using Microsoft.Extensions.Options;
using Validators;

namespace WebAPI.Capabilities;

/// <summary>
/// Configure startup services 
/// </summary>
public static class StartupInjection
{
    /// <summary>
    /// Configure startup services 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IServiceCollection ConfigureInjection(this IServiceCollection services, IConfiguration configuration)
    {
        string? dbConnectionString = configuration.GetConnectionString("PostgreConnection");
        if (string.IsNullOrEmpty(dbConnectionString))
            throw new ArgumentNullException(dbConnectionString, "Postgre connection string not found");

        services.AddHttpClient()
                .Configure<PasswordEncryption>(configuration.GetSection("PasswordEncryption"))
                .AddApplication()
                .AddClients()
                .AddInfrastructure(dbConnectionString)
                .Configure<FontSettings>(configuration.GetSection("FontSettings"))
                .AddPdfGenerator(services.BuildServiceProvider().GetRequiredService<IOptions<FontSettings>>())
                .AddValidations();

        return services;
    }
}
