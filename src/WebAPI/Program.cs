using Serilog;
using WebAPI.Capabilities;
using WebAPI.Middleware;

namespace WebAPI;

/// <summary>
/// 
/// </summary>
public class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();

        builder.Services
            .ConfigureInjection(builder.Configuration)
            .ConfigureLogging(builder.Configuration)
            .ConfigureAutoMapper()
            .ConfigureSwagger();

        builder.Host.UseSerilog();

        builder.Host.UseDefaultServiceProvider((_, serviceProviderOptions) =>
        {
            serviceProviderOptions.ValidateScopes = true;
            serviceProviderOptions.ValidateOnBuild = true;
        });

        var app = builder.Build();

        //custom error handling middleware
        app.UseMiddleware<ErrorChecking>();

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.UseSwaggerWithUI();

        app.Run();
    }
}

