using BillerContracts.Requests.Customer;
using BillerContracts.Requests.Invoice;
using BillerContracts.Requests.Item;
using BillerContracts.Requests.Seller;
using BillerContracts.Requests.User;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Validators;
using Validators.Customer;
using Validators.Invoice;
using Validators.Item;
using Validators.Seller;
using Validators.User;

namespace xUnitTests.Validators;

public class DependencyInjectionTests
{
    public static IEnumerable<object[]> Registrations()
    {
        yield return [typeof(IValidator<UserAddRequest>), typeof(UserAddValidator)];
        yield return [typeof(IValidator<UserUpdateRequest>), typeof(UserUpdateValidator)];
        yield return [typeof(IValidator<UserLoginRequest>), typeof(UserLoginValidator)];
        yield return [typeof(IValidator<SellerAddRequest>), typeof(SellerAddValidator)];
        yield return [typeof(IValidator<SellerUpdateRequest>), typeof(SellerUpdateValidator)];
        yield return [typeof(IValidator<CustomerAddRequest>), typeof(CustomerAddValidator)];
        yield return [typeof(IValidator<CustomerUpdateRequest>), typeof(CustomerUpdateValidator)];
        yield return [typeof(IValidator<ItemAddRequest>), typeof(ItemAddValidator)];
        yield return [typeof(IValidator<ItemUpdateRequest>), typeof(ItemUpdateValidator)];
        yield return [typeof(IValidator<InvoiceAddRequest>), typeof(InvoiceAddValidator)];
        yield return [typeof(IValidator<InvoiceGenerateRequest>), typeof(InvoiceGenerateValidator)];
        yield return [typeof(IValidator<InvoiceUpdateRequest>), typeof(InvoiceUpdateValidator)];
        yield return [typeof(IValidator<InvoiceItemRequest>), typeof(InvoiceItemValidator)];
        yield return [typeof(IValidator<InvoiceItemUpdateRequest>), typeof(InvoiceItemUpdateValidator)];
    }

    [Theory]
    [MemberData(nameof(Registrations))]
    public void AddValidations_ShouldRegisterScopedValidator(Type serviceType, Type implementationType)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddValidations();
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        // Assert
        services.Should().ContainSingle(d => d.ServiceType == serviceType)
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);

        var validator = scope.ServiceProvider.GetService(serviceType);
        validator.Should().NotBeNull().And.BeOfType(implementationType);
    }

    [Fact]
    public void AddValidations_ShouldReturnSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddValidations();

        // Assert
        result.Should().BeSameAs(services);
        services.Should().HaveCount(Registrations().Count());
    }
}
