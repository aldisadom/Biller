using BillerContracts.Requests.Invoice;
using AwesomeAssertions;
using FluentValidation;
using Validators.Invoice;

namespace xUnitTests.Validators.Invoice;

public class InvoiceUpdateValidatorTest
{
    private readonly InvoiceUpdateValidator _validator = new(RepoFonts.CalibriChecker);

    private static InvoiceUpdateRequest ValidRequest() => new()
    {
        Id = Guid.NewGuid(),
        SellerId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        Items =
        [
            new InvoiceItemRequest { Id = Guid.NewGuid(), Quantity = 2, Version = 1 }
        ],
        CreatedDate = new DateOnly(2024, 1, 1),
        DueDate = new DateOnly(2024, 1, 31)
    };

    [Fact]
    public void Validate_GivenValidRequest_IsValid()
    {
        //Arrange
        var request = ValidRequest();

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    public static IEnumerable<TheoryDataRow<Action<InvoiceUpdateRequest>, string, string>> GetEmptyRequiredFieldTestData()
    {
        yield return new(r => r.Id = Guid.Empty, nameof(InvoiceUpdateRequest.Id), "Please specify invoice id") { Label = "empty id is rejected" };
        yield return new(r => r.SellerId = Guid.Empty, nameof(InvoiceUpdateRequest.SellerId), "Please specify seller Id") { Label = "empty seller id is rejected" };
        yield return new(r => r.CustomerId = Guid.Empty, nameof(InvoiceUpdateRequest.CustomerId), "Please specify customer id") { Label = "empty customer id is rejected" };
        yield return new(r => r.Items = [], nameof(InvoiceUpdateRequest.Items), "Please provide at least one item") { Label = "empty item list is rejected" };
    }

    [Theory]
    [MemberData(nameof(GetEmptyRequiredFieldTestData))]
    public void Validate_GivenEmptyRequiredField_IsInvalid(Action<InvoiceUpdateRequest> clearField, string propertyName, string message)
    {
        //Arrange
        var request = ValidRequest();
        clearField(request);

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == propertyName && e.ErrorMessage == message);
    }

    [Fact]
    public void Validate_GivenLineWithoutVersion_IsInvalid()
    {
        //Arrange
        var request = ValidRequest();
        request.Items[0].Version = null;

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == $"{nameof(request.Items)}[0]"
            && e.ErrorMessage == $"Please specify item version for {request.Items[0].Id}");
    }

    [Fact]
    public void Validate_GivenDueDateBeforeCreatedDate_IsInvalid()
    {
        //Arrange
        var request = ValidRequest();
        request.CreatedDate = new DateOnly(2024, 1, 31);
        request.DueDate = new DateOnly(2024, 1, 1);

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.DueDate));
    }

    public static IEnumerable<TheoryDataRow<InvoiceItemRequest>> GetInvalidItemTestData()
    {
        yield return new(new InvoiceItemRequest { Id = Guid.Empty, Quantity = 1 }) { Label = "item with empty id throws" };
        yield return new(new InvoiceItemRequest { Id = Guid.NewGuid(), Quantity = 0 }) { Label = "item with zero quantity throws" };
        yield return new(new InvoiceItemRequest { Id = Guid.NewGuid(), Quantity = -1 }) { Label = "item with negative quantity throws" };
        yield return new(new InvoiceItemRequest { Id = Guid.NewGuid(), Quantity = 1, Version = 0 }) { Label = "item with version zero throws" };
    }

    [Theory]
    [MemberData(nameof(GetInvalidItemTestData))]
    public void Validate_GivenInvalidItem_ThrowsValidationException(InvoiceItemRequest item)
    {
        //Arrange
        var request = ValidRequest();
        request.Items = [item];

        //Act
        //Assert
        Assert.Throws<ValidationException>(() => _validator.Validate(request));
    }

    public static IEnumerable<TheoryDataRow<Action<InvoiceUpdateRequest>, string, string>> GetPdfTextFieldTestData()
    {
        yield return new(r => r.Comments = "Dragon 🐉", nameof(InvoiceUpdateRequest.Comments), "Comments") { Label = "emoji in invoice comment is rejected and named" };
    }

    [Theory]
    [MemberData(nameof(GetPdfTextFieldTestData))]
    public void Validate_GivenCharacterThePdfCannotShow_IsInvalid(Action<InvoiceUpdateRequest> setField, string propertyName, string displayName)
    {
        //Arrange
        var request = ValidRequest();
        setField(request);

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == propertyName &&
            e.ErrorMessage == $"Please use only characters the invoice PDF can show in {displayName}. Can't show: 🐉 (U+1F409)");
    }

    [Fact]
    public void Validate_GivenLithuanianText_IsValid()
    {
        //Arrange
        var request = ValidRequest();
        request.Comments = "Ąžuolų šėtra €5";

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GivenItemCommentThePdfCannotShow_ThrowsValidationExceptionNamingTheCharacter()
    {
        //Arrange
        var request = ValidRequest();
        request.Items = [new InvoiceItemRequest { Id = Guid.NewGuid(), Quantity = 2, Comments = "Dragon 🐉" }];

        //Act
        Action act = () => _validator.Validate(request);

        //Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("Please use only characters the invoice PDF can show in Comments. Can't show: 🐉 (U+1F409)");
    }
}
