using Application.Helpers.PriceToWords;
using Application.Helpers.Texts;
using Application.Models;
using Application.Models.Invoice.InvoiceGenerationModels;
using Application.Models.Invoice.Texts;
using Contracts.Enums;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Application.Services;

public interface IInvoiceDocumentFactory
{
    string GeneratePdf(DocumentType documentType, Language languageCode, InvoiceModel invoiceData);
}

public class InvoiceDocumentFactory : IInvoiceDocumentFactory
{
    IPriceToWordsFactory _priceToWordsFactory;

    public InvoiceDocumentFactory(IPriceToWordsFactory priceToWordsFactory)
    {
        _priceToWordsFactory = priceToWordsFactory;
    }

    public string GeneratePdf(DocumentType documentType, Language languageCode, InvoiceModel invoiceData)
    {
        var texts = GetTexts(languageCode);        
        var document = GetDocument(languageCode, documentType, invoiceData, texts);
        
        string path = invoiceData.GenerateFileLocation();
        document.GeneratePdf(path);

        return path;
    }

    private static IInvoiceTexts GetTexts(Language languageCode)
    {
        return languageCode switch
        {
            Language.LT => new InvoiceTextsLT(),
            Language.EN => new InvoiceTextsEN(),
            _ => throw new NotSupportedException($"Languge {languageCode} is not supported")
        };
    }

    private IDocument GetDocument(Language languageCode, DocumentType documentType, InvoiceModel invoiceData, IInvoiceTexts texts)
    {
        var priceToWords = _priceToWordsFactory.GetConverter(languageCode);
        return documentType switch
        {
            DocumentType.Invoice => new InvoiceDocumentSF(invoiceData, priceToWords, texts),
            DocumentType.JobDoneAct => new InvoiceDocumentADA(invoiceData, priceToWords, texts),
            _ => throw new NotSupportedException($"Document type {documentType} is not supported")
        };
    }
}
