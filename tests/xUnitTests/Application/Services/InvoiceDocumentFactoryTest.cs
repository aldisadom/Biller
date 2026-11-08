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

namespace xUnitTests.Application.Services;

public class InvoiceDocumentFactoryTest
{
    private readonly Mock<IPriceToWordsFactory> _priceToWordsFactoryMock;
    private readonly Mock<IPriceToWords> _priceToWordsMock;

    private readonly InvoiceDocumentFactory _invoiceDocumentFactory;

    // Same font and QuestPDF settings as the app: the documents use the font family "calibri", and
    // since QuestPDF 2026.9 a missing family throws.
    static InvoiceDocumentFactoryTest()
    {
        new ServiceCollection().AddPdfGenerator(Options.Create(new FontSettings
        {
            Fonts = [new FontSetting { Name = "calibri", Path = RepoFonts.CalibriPath }]
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
}
