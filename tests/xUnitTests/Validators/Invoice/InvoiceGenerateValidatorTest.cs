using BillerContracts.Enums;
using BillerContracts.Requests.Invoice;
using FluentAssertions;
using Validators.Invoice;

namespace xUnitTests.Validators.Invoice;

public class InvoiceGenerateValidatorTest
{
    private readonly InvoiceGenerateValidator _validator = new();

    [Fact]
    public void Validate_GivenEmptyId_IsInvalid()
    {
        //Arrange
        var request = new InvoiceGenerateRequest { Id = Guid.Empty };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Id));
    }

    [Theory]
    [InlineData(Language.LT, DocumentType.Invoice, Label = "LT invoice is valid")]
    [InlineData(Language.LT, DocumentType.JobDoneAct, Label = "LT job done act is valid")]
    [InlineData(Language.EN, DocumentType.Invoice, Label = "EN invoice is valid")]
    [InlineData(Language.EN, DocumentType.JobDoneAct, Label = "EN job done act is valid")]
    public void Validate_GivenDefinedLanguageAndDocumentType_IsValid(Language languageCode, DocumentType documentType)
    {
        //Arrange
        var request = new InvoiceGenerateRequest { Id = Guid.NewGuid(), LanguageCode = languageCode, DocumentType = documentType };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GivenUndefinedLanguageCode_IsInvalid()
    {
        //Arrange
        var request = new InvoiceGenerateRequest { Id = Guid.NewGuid(), LanguageCode = (Language)999 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.LanguageCode));
    }

    [Fact]
    public void Validate_GivenUndefinedDocumentType_IsInvalid()
    {
        //Arrange
        var request = new InvoiceGenerateRequest { Id = Guid.NewGuid(), DocumentType = (DocumentType)999 };

        //Act
        var result = _validator.Validate(request);

        //Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.DocumentType));
    }
}
