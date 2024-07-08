﻿using AutoMapper;
using WebAPI.MappingProfiles;
using Mapster;

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
            mc.AddProfile(new ItemMappingProfile());
            mc.AddProfile(new CustomerMappingProfile());
            mc.AddProfile(new SellerMappingProfile());
            mc.AddProfile(new InvoiceDataMappingProfile());
        });

        mapperConfig.AssertConfigurationIsValid();

        IMapper mapper = mapperConfig.CreateMapper();

        services.AddSingleton(mapper);

        //services.AddMapster();
        
        return services;
    }
}
