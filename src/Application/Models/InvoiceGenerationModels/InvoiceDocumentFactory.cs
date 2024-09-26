using Application.Helpers.PriceToWords;
using Common.Enums;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Application.Models.InvoiceGenerationModels;

public interface IInvoiceDocumentFactory
{
    void GeneratePdf(DocumentType documentType, Language languageCode, InvoiceModel invoiceData);
}

public class InvoiceDocumentFactory : IInvoiceDocumentFactory
{
    IPriceToWordsFactory _priceToWordsFactory;

    public InvoiceDocumentFactory(IPriceToWordsFactory priceToWordsFactory)
    {
        _priceToWordsFactory = priceToWordsFactory;
    }

    public void GeneratePdf(DocumentType documentType, Language languageCode, InvoiceModel invoiceData)
    {
        IDocument document;
        switch (documentType)
        {
            case DocumentType.Invoice:
                document = new InvoiceDocumentSF(invoiceData, _priceToWordsFactory.GetConverter(languageCode));
                break;
            case DocumentType.JobDoneAct:
                document = new InvoiceDocumentADA(invoiceData, _priceToWordsFactory.GetConverter(languageCode));
                break;
            default:
                throw new NotSupportedException($"Language {documentType} is not supported");

        }
        document.GeneratePdf();
    }
}
