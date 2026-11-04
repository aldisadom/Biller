using Application.Models;
using FluentAssertions;
using Validators.Invoice;

namespace xUnitTests.Validators.Invoice;

public class InvoiceModelUpdateValidatorTest
{
    private readonly InvoiceModelUpdateValidator _validator = new();

    private static InvoiceModel ValidModel() => new()
    {
        Id = Guid.NewGuid(),
        SellerId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        Items = [new() { Id = Guid.NewGuid() }]
    };

    [Fact]
    public void Validate_GivenValidModel_IsValid()
    {
        //Arrange
        InvoiceModel model = ValidModel();

        //Act
        var result = _validator.Validate(model);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    public static IEnumerable<TheoryDataRow<Action<InvoiceModel>, string>> GetEmptyIdTestData()
    {
        yield return new(m => m.Id = Guid.Empty, nameof(InvoiceModel.Id)) { Label = "empty invoice id is rejected" };
        yield return new(m => m.SellerId = Guid.Empty, nameof(InvoiceModel.SellerId)) { Label = "empty seller id is rejected" };
        yield return new(m => m.CustomerId = Guid.Empty, nameof(InvoiceModel.CustomerId)) { Label = "empty customer id is rejected" };
        yield return new(m => m.Items = null, nameof(InvoiceModel.Items)) { Label = "missing item list is rejected" };
        yield return new(m => m.Items![0].Id = Guid.Empty, $"{nameof(InvoiceModel.Items)}[0]") { Label = "empty item id is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyIdTestData))]
    public void Validate_GivenEmptyId_IsInvalid(Action<InvoiceModel> breakModel, string propertyName)
    {
        //Arrange
        InvoiceModel model = ValidModel();
        breakModel(model);

        //Act
        var result = _validator.Validate(model);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == propertyName);
    }
}
