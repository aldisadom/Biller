using Microsoft.Extensions.DependencyInjection;

namespace Clients;

public static class DependencyInjection
{
    public static IServiceCollection AddClients(this IServiceCollection services)
    {
        //inject client

        return services;
    }
}
