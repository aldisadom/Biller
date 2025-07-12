using Application;
using Application.Helpers.PriceToWords;
using Application.Interfaces;
using Application.Models.InvoiceGenerationModels;
using Application.Services;
using AutoMapper;
using Clients;
using Domain.Repositories;
using Infrastructure;
using Infrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.MappingProfiles;

namespace xUnitTests.Application;

public class DependencyInjectionTests
{
    const string _dbConnectionString = "Host=db;Username=postgres;Password=postgres;Database=Data";

    [Fact]
    public void AddInfrastructure_ShouldRegisterInfrastructure()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(_dbConnectionString);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        using var scope = serviceProvider.CreateScope();
        var scopedServiceProvider = scope.ServiceProvider;

        var itemRepository = scopedServiceProvider.GetService<IItemRepository>();
        Assert.NotNull(itemRepository);
        Assert.IsType<ItemRepository>(itemRepository);

        var customerRepository = scopedServiceProvider.GetService<ICustomerRepository>();
        Assert.NotNull(customerRepository);
        Assert.IsType<CustomerRepository>(customerRepository);

        var sellerRepository = scopedServiceProvider.GetService<ISellerRepository>();
        Assert.NotNull(sellerRepository);
        Assert.IsType<SellerRepository>(sellerRepository);
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        services.Configure<Domain.IOptions.PasswordEncryption>(options =>{options.Salt = "your-salt-value";});

        // Act
        services.AddApplication();
        services.AddInfrastructure(_dbConnectionString);

        var mapperConfig = new MapperConfiguration(mc =>{});
        IMapper mapper = mapperConfig.CreateMapper();

        services.AddSingleton(mapper);

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        using var scope = serviceProvider.CreateScope();
        var scopedServiceProvider = scope.ServiceProvider;

        var itemService = scopedServiceProvider.GetRequiredService<IItemService>();
        Assert.NotNull(itemService);
        Assert.IsType<ItemService>(itemService);

        var customerService = scopedServiceProvider.GetService<ICustomerService>();
        Assert.NotNull(customerService);
        Assert.IsType<CustomerService>(customerService);

        var sellerService = scopedServiceProvider.GetService<ISellerService>();
        Assert.NotNull(sellerService);
        Assert.IsType<SellerService>(sellerService);

        var userService = scopedServiceProvider.GetService<IUserService>();
        Assert.NotNull(userService);
        Assert.IsType<UserService>(userService);

        var passwordEncryptionService = scopedServiceProvider.GetService<IPasswordEncryptionService>();
        Assert.NotNull(passwordEncryptionService);
        Assert.IsType<PasswordEncryptionService>(passwordEncryptionService);

        var invoiceService = scopedServiceProvider.GetService<IInvoiceService>();
        Assert.NotNull(invoiceService);
        Assert.IsType<InvoiceService>(invoiceService);

        var invoiceDocumentFactory = scopedServiceProvider.GetService<IInvoiceDocumentFactory>();
        Assert.NotNull(invoiceDocumentFactory);
        Assert.IsType<InvoiceDocumentFactory>(invoiceDocumentFactory);

        var priceToWordsFactory = scopedServiceProvider.GetService<IPriceToWordsFactory>();
        Assert.NotNull(priceToWordsFactory);
        Assert.IsType<PriceToWordsFactory>(priceToWordsFactory);
    }

    [Fact]
    public void AddClients_ShouldRegisterClientServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var initialServiceCount = services.Count;

        // Act
        services.AddClients();
        var finalServiceCount = services.Count;

        // Assert
        Assert.Equal(initialServiceCount, finalServiceCount);
    }

}
