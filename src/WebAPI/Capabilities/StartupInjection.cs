﻿using Application;
using Clients;
using Domain.IOptions;
using Infrastructure;
using Microsoft.Extensions.Configuration;

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
        services.AddHttpClient();

        services.Configure<PasswordEncryption>(configuration.GetSection("PasswordEncryption"));

        services.AddApplication();
        services.AddClients();

        string dbConnectionString = configuration.GetConnectionString("PostgreConnection")
            ?? throw new ArgumentNullException("Postgre connection string not found");

        services.AddInfrastructure(dbConnectionString);

        return services;
    }
}
