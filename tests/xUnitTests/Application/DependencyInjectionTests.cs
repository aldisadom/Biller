using Application;
using Application.Helpers.PriceToWords;
using Application.Interfaces;
using Application.Services;
using Domain.IOptions;
using Domain.Repositories;
using FluentAssertions;
using FluentAssertions.Execution;
using Infrastructure;
using Infrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;

namespace xUnitTests.Application;

public class DependencyInjectionTests
{
    const string _dbConnectionString = "Host=db;Username=postgres;Password=postgres;Database=Data";

    private static readonly Dictionary<Type, (Type Implementation, ServiceLifetime Lifetime)> _expectedInfrastructureRegistrations = new()
    {
        [typeof(IDbConnection)] = (typeof(NpgsqlConnection), ServiceLifetime.Scoped),
        [typeof(IItemRepository)] = (typeof(ItemRepository), ServiceLifetime.Scoped),
        [typeof(ICustomerRepository)] = (typeof(CustomerRepository), ServiceLifetime.Scoped),
        [typeof(ISellerRepository)] = (typeof(SellerRepository), ServiceLifetime.Scoped),
        [typeof(IInvoiceRepository)] = (typeof(InvoiceRepository), ServiceLifetime.Scoped),
        [typeof(IUserRepository)] = (typeof(UserRepository), ServiceLifetime.Scoped),
    };

    private static readonly Dictionary<Type, (Type Implementation, ServiceLifetime Lifetime)> _expectedApplicationRegistrations = new()
    {
        [typeof(IItemService)] = (typeof(ItemService), ServiceLifetime.Scoped),
        [typeof(ICustomerService)] = (typeof(CustomerService), ServiceLifetime.Scoped),
        [typeof(ISellerService)] = (typeof(SellerService), ServiceLifetime.Scoped),
        [typeof(IUserService)] = (typeof(UserService), ServiceLifetime.Scoped),
        [typeof(IPasswordEncryptionService)] = (typeof(PasswordEncryptionService), ServiceLifetime.Scoped),
        [typeof(IInvoiceService)] = (typeof(InvoiceService), ServiceLifetime.Scoped),
        [typeof(IInvoiceDocumentFactory)] = (typeof(InvoiceDocumentFactory), ServiceLifetime.Singleton),
        [typeof(IPriceToWordsFactory)] = (typeof(PriceToWordsFactory), ServiceLifetime.Singleton),
    };

    private static void AssertRegistrations(IServiceCollection registered, IServiceProvider resolver,
        Dictionary<Type, (Type Implementation, ServiceLifetime Lifetime)> expected)
    {
        registered.Select(d => d.ServiceType).Should().BeEquivalentTo(expected.Keys,
            "only the expected services should be registered, each once");

        foreach (var (serviceType, (implementationType, lifetime)) in expected)
        {
            registered.Where(d => d.ServiceType == serviceType)
                .Should().AllSatisfy(d => d.Lifetime.Should().Be(lifetime, "{0} should be {1}", serviceType.Name, lifetime));

            resolver.GetService(serviceType)
                .Should().BeOfType(implementationType, "{0} should resolve to {1}", serviceType.Name, implementationType.Name);
        }
    }

    [Fact]
    public void AddInfrastructure_GivenConnectionString_RegistersExactlyAllRepositories()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        var result = services.AddInfrastructure(_dbConnectionString);
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        //Assert
        using var _ = new AssertionScope();

        result.Should().BeSameAs(services);
        AssertRegistrations(services, scope.ServiceProvider, _expectedInfrastructureRegistrations);
    }

    [Fact]
    public void AddApplication_GivenServiceCollection_RegistersExactlyAllServices()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        var result = services.AddApplication();

        // the services need the password options and the repositories to be resolved
        IServiceCollection resolvable = new ServiceCollection();
        resolvable.Configure<PasswordEncryption>(options => { options.Pepper = "pepper"; });
        resolvable.AddInfrastructure(_dbConnectionString);
        foreach (var descriptor in services)
            resolvable.Add(descriptor);

        using var serviceProvider = resolvable.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        //Assert
        using var _ = new AssertionScope();

        result.Should().BeSameAs(services);
        AssertRegistrations(services, scope.ServiceProvider, _expectedApplicationRegistrations);
    }

    [Fact]
    public void AddPdfGenerator_GivenValidFontSettings_RegistersFonts()
    {
        //Arrange
        var services = new ServiceCollection();

        var fontPath = Path.Combine("..", "..", "..", "..", "..", "Fonts", "calibri.ttf");
        var fontSettings = Options.Create(new FontSettings
        {
            Fonts = [
                new FontSetting { Name = "calibri", Path = fontPath }
                ]
        });

        //Act
        var result = services.AddPdfGenerator(fontSettings);

        //Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddPdfGenerator_GivenNullOptions_ThrowsArgumentNullException()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        Action act = () => services.AddPdfGenerator(null!);

        //Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*Font settings cannot be null*");
    }

    [Theory]
    [InlineData(null, "calibri", Label = "missing font path throws")]
    [InlineData("", "calibri", Label = "empty font path throws")]
    [InlineData("some-path.ttf", null, Label = "missing font name throws")]
    [InlineData("some-path.ttf", "", Label = "empty font name throws")]
    public void AddPdfGenerator_GivenEmptyFontPathOrName_ThrowsArgumentException(string? path, string? name)
    {
        //Arrange
        var services = new ServiceCollection();
        var fontSettings = Options.Create(new FontSettings
        {
            Fonts = [
                new FontSetting { Name = name!, Path = path! }
            ]
        });

        //Act
        Action act = () => services.AddPdfGenerator(fontSettings);

        //Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Font path or name cannot be null or empty*");
    }

    public static IEnumerable<TheoryDataRow<FontSettings>> GetFontSettingsTestData()
    {
        yield return new(null!) { Label = "font settings are null, throws" };
        yield return new(new FontSettings { Fonts = null! }) { Label = "font list is null, throws" };
        yield return new(new FontSettings { Fonts = [] }) { Label = "font list is empty, throws" };
    }

    [Theory]
    [MemberData(nameof(GetFontSettingsTestData))]
    public void AddPdfGenerator_GivenNoFonts_ThrowsArgumentException(FontSettings fontSettings)
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        Action act = () => services.AddPdfGenerator(Options.Create(fontSettings));

        //Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Font settings must contain at least one font*");
    }

    [Fact]
    public void AddPdfGenerator_GivenMissingFontFile_ThrowsWithFontPath()
    {
        //Arrange
        var services = new ServiceCollection();
        var fontSettings = new FontSettings
        {
            Fonts = [
                new FontSetting { Name = "calibri", Path = "nonexistent-path.ttf" }
                ]
        };

        //Act
        Action act = () => services.AddPdfGenerator(Options.Create(fontSettings));

        //Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*Font calibri file not found at path: nonexistent-path.ttf*");
    }
}
