using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;

namespace WebAPI.Capabilities;

/// <summary>
/// Configure swagger services 
/// </summary>
public static class StartupSwagger
{
    private static readonly string _apiName = "Billio";
    private static readonly string _apiVersion = "v1";

    /// <summary>
    /// Configure swagger services 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        // Add framework services.
        services.AddMvc();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(_apiVersion, new OpenApiInfo { Title = _apiName, Version = _apiVersion });

            c.ExampleFilters();
            // Set the comments path for the Swagger JSON and UI.
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);

            c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>(); // Adds "(Auth)" to the summary so that you can see which endpoints have Authorization
        });

        services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());

        return services;
    }

    /// <summary>
    /// Configure swagger services 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureSwaggerWithOauth(this IServiceCollection services)
    {
        // Add framework services.
        services.AddMvc();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(_apiVersion, new OpenApiInfo { Title = _apiName, Version = _apiVersion });

            c.ExampleFilters();
            // Set the comments path for the Swagger JSON and UI.
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);

            c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>(); // Adds "(Auth)" to the summary so that you can see which endpoints have Authorization
                                                                          // or use the generic method, e.g. c.OperationFilter<AppendAuthorizeToSummaryOperationFilter<MyCustomAttribute>>();

            // add Security information to each operation for OAuth2
            c.OperationFilter<SecurityRequirementsOperationFilter>();
            // or use the generic method, e.g. c.OperationFilter<SecurityRequirementsOperationFilter<MyCustomAttribute>>();

            // if you're using the SecurityRequirementsOperationFilter, you also need to tell Swashbuckle you're using OAuth2
            c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Description = "Standard Authorization header using the Bearer scheme. Example: \"bearer {token}\"",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey
            });
        });

        services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());

        return services;
    }

    /// <summary>
    /// Configure swagger application 
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseSwaggerWithUI(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{_apiName} {_apiVersion}");
        });

        return app;
    }
}
