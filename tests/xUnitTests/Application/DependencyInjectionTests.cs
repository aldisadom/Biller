using Application;
using Application.Helpers.PriceToWords;
using Application.Interfaces;
using Application.Models.InvoiceGenerationModels;
using Application.Services;
using AutoMapper;
using Clients;
using Domain.IOptions;
using Domain.Repositories;
using Infrastructure;
using Infrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        services.Configure<Domain.IOptions.PasswordEncryption>(options => { options.Salt = "your-salt-value"; });

        // Act
        services.AddApplication();
        services.AddInfrastructure(_dbConnectionString);

        var mapperConfig = new MapperConfiguration(mc => { });
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

    [Fact]
    public void AddPdfGenerator_ShouldRegisterFonts_WhenFontSettingsAreValid()
    {
        // Arrange
        var services = new ServiceCollection();

        var fontPath = Path.Combine("..", "..", "..", "..", "..", "Fonts", "calibri.ttf");
        var fontSettings = Options.Create(new FontSettings
        {
            Fonts = [
                new FontSetting { Name = "calibri", Path = fontPath }
                ]
        });

        // Act
        var exception = Record.Exception(() => services.AddPdfGenerator(fontSettings));

        // Assert
        Assert.Null(exception); // Ensure no exception is thrown
    }

    [Fact]
    public void AddPdfGenerator_ShouldThrow_WhenFontSettingsAreNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentNullException>(() => services.AddPdfGenerator(null));
        Assert.Contains("Font settings cannot be null", exception.Message);
    }

    public static IEnumerable<object[]> GetFontSettingsTestData()
    {
        yield return new object[] { null! };
        yield return new object[] { new FontSettings { Fonts = null! } };
        yield return new object[] { new FontSettings { Fonts = [] } };
    }

    [Theory]
    [MemberData(nameof(GetFontSettingsTestData))]
    public void AddPdfGenerator_ShouldThrow_WhenFontSettingsAreEmpty(FontSettings fontSettings)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentException>(() => services.AddPdfGenerator(Options.Create(fontSettings)));
        Assert.Contains("Font settings must contain at least one font", exception.Message);
    }

    [Fact]
    public void AddPdfGenerator_ShouldThrow_WhenFontIsNotFound()
    {
        // Arrange
        var services = new ServiceCollection();
        var fontSettings = new FontSettings
        {
            Fonts = [
                new FontSetting { Name = "calibri", Path = "nonexistent-path.ttf" }
                ]
        };

        // Act
        // Assert
        var exception = Assert.Throws<FileNotFoundException>(() => services.AddPdfGenerator(Options.Create(fontSettings)));
        Assert.Contains("Font calibri file not found at path: nonexistent-path.ttf", exception.Message);
    }

    [Fact]
    public void AddPdfGenerator_ShouldThrow_WhenFontFileDoesNotExist()
    {
        // Arrange
        var services = new ServiceCollection();
        var fontSettings = Options.Create(new FontSettings
        {
            Fonts =
            [
                new FontSetting { Name = "calibri", Path = "nonexistent-path.ttf" }
            ]
        });

        // Act
        // Assert
        var exception = Assert.Throws<FileNotFoundException>(() => services.AddPdfGenerator(fontSettings));
        Assert.Contains("Font calibri file not found", exception.Message);
    }
}
