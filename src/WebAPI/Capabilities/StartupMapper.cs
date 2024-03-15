using AutoMapper;
using WebAPI.MappingProfiles;


namespace WebAPI.Capabilities;

/// <summary>
/// Configure startup services 
/// </summary>
public static class StartupMapper
{
    /// <summary>
    /// Configure startup services
    /// </summary>
    /// <param name="services"></param>
    public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
    {
        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new UserMappingProfile());
            mc.AddProfile(new InvoiceItemMappingProfile());
            mc.AddProfile(new InvoiceAddressMappingProfile());
            mc.AddProfile(new InvoiceDataMappingProfile());
        });

        mapperConfig.AssertConfigurationIsValid();

        IMapper mapper = mapperConfig.CreateMapper();

        services.AddSingleton(mapper);

        return services;
    }
}
