using Application.Helpers.PriceToWords;
using Application.Models;
using Application.Services;
using BillerContracts.Enums;
using AwesomeAssertions;
using Domain.IOptions;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Domain.Exceptions;

namespace xUnitTests.Application.Services;

[Collection(QuestPdfCollection.Name)]
public class InvoiceDocumentFactoryTest
{
    private readonly Mock<IPriceToWordsFactory> _priceToWordsFactoryMock;
    private readonly Mock<IPriceToWords> _priceToWordsMock;

    private readonly InvoiceDocumentFactory _invoiceDocumentFactory;

    // Same font and QuestPDF settings as the app: the documents use the font family "calibri", and a
    // missing family or a character Calibri can't draw throws.
    static InvoiceDocumentFactoryTest()
    {
        new ServiceCollection().AddPdfGenerator(Options.Create(new FontSettings
        {
            Fonts = [new FontSetting { Path = RepoFonts.CalibriPath }]
        }));
    }

    public InvoiceDocumentFactoryTest()
    {
        _priceToWordsFactoryMock = new Mock<IPriceToWordsFactory>(MockBehavior.Strict);
        _priceToWordsMock = new Mock<IPriceToWords>(MockBehavior.Strict);

        _invoiceDocumentFactory = new InvoiceDocumentFactory(_priceToWordsFactoryMock.Object);
    }

    [Theory]
    [InlineData(DocumentType.Invoice, Language.LT, Label = "lithuanian invoice is returned from its first byte")]
    [InlineData(DocumentType.Invoice, Language.EN, Label = "english invoice is returned from its first byte")]
    [InlineData(DocumentType.JobDoneAct, Language.LT, Label = "lithuanian job done act is returned from its first byte")]
    [InlineData(DocumentType.JobDoneAct, Language.EN, Label = "english job done act is returned from its first byte")]
    public void GeneratePdf_GivenInvoice_ReturnsPdfFromStart(DocumentType documentType, Language languageCode)
    {
        //Arrange
        InvoiceModelFull invoice = new()
        {
            SeriesName = "WF",
            InvoiceNumber = 5,
            CreatedDate = new DateOnly(2026, 10, 1),
            DueDate = new DateOnly(2026, 10, 11),
            Items = [new() { Name = "Iron throne", Price = 10m, Quantity = 2 }],
            User = new() { Name = "Tyrion", LastName = "Lannister" },
            Seller = new() { CompanyName = "Lannister gold" },
            Customer = new() { CompanyName = "Winterfell" }
        };

        _priceToWordsFactoryMock.Setup(m => m.GetConverter(languageCode))
            .Returns(_priceToWordsMock.Object);
        _priceToWordsMock.Setup(m => m.Decode(20m))
            .Returns("twenty");

        //Act
        MemoryStream result = _invoiceDocumentFactory.GeneratePdf(documentType, languageCode, invoice);

        //Assert
        result.Position.Should().Be(0);
        byte[] header = new byte[5];
        result.Read(header, 0, header.Length);
        header.Should().Equal("%PDF-"u8.ToArray());

        _priceToWordsFactoryMock.Verify(m => m.GetConverter(languageCode), Times.Once());
        _priceToWordsMock.Verify(m => m.Decode(20m), Times.Once());
    }

    [Theory]
    [InlineData(DocumentType.Invoice, Language.LT, Label = "lithuanian letters and the euro sign render in the invoice")]
    [InlineData(DocumentType.JobDoneAct, Language.LT, Label = "lithuanian letters and the euro sign render in the job done act")]
    public void GeneratePdf_GivenLithuanianLetters_ReturnsPdf(DocumentType documentType, Language languageCode)
    {
        //Arrange
        const string letters = "ĄČĘĖĮŠŲŪŽ ąčęėįšųūž €";
        InvoiceModelFull invoice = new()
        {
            SeriesName = "WF",
            InvoiceNumber = 5,
            CreatedDate = new DateOnly(2026, 10, 1),
            DueDate = new DateOnly(2026, 10, 11),
            Items = [new() { Name = letters, Price = 10m, Quantity = 2 }],
            User = new() { Name = "Tyrion", LastName = "Lannister" },
            Seller = new() { CompanyName = letters },
            Customer = new() { CompanyName = "Winterfell" }
        };

        _priceToWordsFactoryMock.Setup(m => m.GetConverter(languageCode))
            .Returns(_priceToWordsMock.Object);
        _priceToWordsMock.Setup(m => m.Decode(20m))
            .Returns("dvidešimt");

        //Act
        MemoryStream result = _invoiceDocumentFactory.GeneratePdf(documentType, languageCode, invoice);

        //Assert
        result.Length.Should().BeGreaterThan(0);

        _priceToWordsFactoryMock.Verify(m => m.GetConverter(languageCode), Times.Once());
        _priceToWordsMock.Verify(m => m.Decode(20m), Times.Once());
    }

    [Theory]
    [InlineData(DocumentType.Invoice, "Dragon 🐉", "🐉", Label = "emoji in an invoice item name throws and is named")]
    [InlineData(DocumentType.Invoice, "Iron throne 鐵王座", "鐵", Label = "chinese characters in an invoice item name throw and are named")]
    [InlineData(DocumentType.JobDoneAct, "Dragon 🐉", "🐉", Label = "emoji in a job done act item name throws and is named")]
    public void GeneratePdf_GivenCharacterCalibriCannotDraw_ThrowsDocumentTextExceptionWithoutTheText(DocumentType documentType, string itemName, string missingCharacter)
    {
        //Arrange
        InvoiceModelFull invoice = new()
        {
            SeriesName = "WF",
            InvoiceNumber = 5,
            CreatedDate = new DateOnly(2026, 10, 1),
            DueDate = new DateOnly(2026, 10, 11),
            Items = [new() { Name = itemName, Price = 10m, Quantity = 2 }],
            User = new() { Name = "Tyrion", LastName = "Lannister" },
            Seller = new() { CompanyName = "Lannister gold" },
            Customer = new() { CompanyName = "Winterfell" }
        };

        _priceToWordsFactoryMock.Setup(m => m.GetConverter(Language.EN))
            .Returns(_priceToWordsMock.Object);
        _priceToWordsMock.Setup(m => m.Decode(20m))
            .Returns("twenty");

        //Act
        Action act = () => _invoiceDocumentFactory.GeneratePdf(documentType, Language.EN, invoice);

        //Assert
        DocumentTextException exception = act.Should().Throw<DocumentTextException>()
            .WithMessage("The invoice has characters the invoice PDF can't show: *").Which;
        exception.Message.Should().Contain(missingCharacter, "the user has to know which character to remove");
        exception.Message.Should().NotContainAny(["Dragon", "Iron throne"], "the message is logged, so it leaves the document text out");
        exception.InnerException.Should().BeNull("QuestPDF's exception holds the whole document text");

        _priceToWordsFactoryMock.Verify(m => m.GetConverter(Language.EN), Times.Once());
        _priceToWordsMock.Verify(m => m.Decode(20m), Times.Once());
    }
}
