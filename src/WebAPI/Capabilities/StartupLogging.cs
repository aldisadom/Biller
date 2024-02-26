using Serilog;

namespace WebAPI.Capabilities;

/// <summary>
/// Configure logging services 
/// </summary>
public static class StartupLogging
{
    /// <summary>
    /// Configure logging services 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureLogging(this IServiceCollection services, IConfiguration configuration)
    {
        //change logger
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        return services;
    }
}
