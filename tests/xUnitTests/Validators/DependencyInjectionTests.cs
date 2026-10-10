using BillerContracts.Requests.Customer;
using BillerContracts.Requests.Invoice;
using BillerContracts.Requests.Item;
using BillerContracts.Requests.Seller;
using BillerContracts.Requests.User;
using FluentAssertions;
using FluentAssertions.Execution;
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
    private static readonly Dictionary<Type, Type> _expectedRegistrations = new()
    {
        [typeof(IValidator<UserAddRequest>)] = typeof(UserAddValidator),
        [typeof(IValidator<UserUpdateRequest>)] = typeof(UserUpdateValidator),
        [typeof(IValidator<UserLoginRequest>)] = typeof(UserLoginValidator),
        [typeof(IValidator<SellerAddRequest>)] = typeof(SellerAddValidator),
        [typeof(IValidator<SellerUpdateRequest>)] = typeof(SellerUpdateValidator),
        [typeof(IValidator<CustomerAddRequest>)] = typeof(CustomerAddValidator),
        [typeof(IValidator<CustomerUpdateRequest>)] = typeof(CustomerUpdateValidator),
        [typeof(IValidator<ItemAddRequest>)] = typeof(ItemAddValidator),
        [typeof(IValidator<ItemUpdateRequest>)] = typeof(ItemUpdateValidator),
        [typeof(IValidator<InvoiceAddRequest>)] = typeof(InvoiceAddValidator),
        [typeof(IValidator<InvoiceGenerateRequest>)] = typeof(InvoiceGenerateValidator),
        [typeof(IValidator<InvoiceUpdateRequest>)] = typeof(InvoiceUpdateValidator),
        [typeof(IValidator<InvoiceItemRequest>)] = typeof(InvoiceItemValidator),
        [typeof(IValidator<InvoiceItemUpdateRequest>)] = typeof(InvoiceItemUpdateValidator),
    };

    [Fact]
    public void AddValidations_ShouldRegisterExactlyAllValidators()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddValidations();
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        // Assert
        using var _ = new AssertionScope();

        result.Should().BeSameAs(services);
        services.Select(d => d.ServiceType).Should().BeEquivalentTo(_expectedRegistrations.Keys,
            "AddValidations should register only the expected validators, each once");

        foreach (var (serviceType, implementationType) in _expectedRegistrations)
        {
            string request = serviceType.GenericTypeArguments[0].Name;

            services.Where(d => d.ServiceType == serviceType)
                .Should().AllSatisfy(d => d.Lifetime.Should().Be(ServiceLifetime.Scoped, "the {0} validator should be scoped", request));

            scope.ServiceProvider.GetService(serviceType)
                .Should().BeOfType(implementationType, "the {0} validator should resolve to {1}", request, implementationType.Name);
        }
    }
}
