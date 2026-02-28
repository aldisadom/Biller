using Application.Helpers.PriceToWords;
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
        var texts = languageCode switch
        {
            Language.LT => new InvoiceTextsLT(),
            _ => throw new NotSupportedException($"Document languge {languageCode} is not supported")
        };
        
        IDocument document;
        switch (documentType)
        {
            case DocumentType.Invoice:
                document = new InvoiceDocumentSF(invoiceData, _priceToWordsFactory.GetConverter(languageCode), texts);
                break;
            case DocumentType.JobDoneAct:
                document = new InvoiceDocumentADA(invoiceData, _priceToWordsFactory.GetConverter(languageCode), texts);
                break;
            default:
                throw new NotSupportedException($"Language {documentType} is not supported");

        }
        string path = invoiceData.GenerateFileLocation();
        document.GeneratePdf(path);

        return path;
    }
}
