using Application.Helpers.Invoice;
using Application.Helpers.PriceToWords;
using Application.Models;
using Application.Models.Invoice.Documents;
using BillerContracts.Enums;
using Domain.Exceptions;
using QuestPDF.Drawing.Exceptions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Application.Services;

public interface IInvoiceDocumentFactory
{
    MemoryStream GeneratePdf(DocumentType documentType, Language languageCode, InvoiceModelFull invoiceData);
}

public class InvoiceDocumentFactory : IInvoiceDocumentFactory
{
    private readonly IPriceToWordsFactory _priceToWordsFactory;

    public InvoiceDocumentFactory(IPriceToWordsFactory priceToWordsFactory)
    {
        _priceToWordsFactory = priceToWordsFactory;
    }

    public MemoryStream GeneratePdf(DocumentType documentType, Language languageCode, InvoiceModelFull invoiceData)
    {
        var texts = GetTexts(languageCode);
        var document = GetDocument(languageCode, documentType, invoiceData, texts);

        MemoryStream stream = new();
        try
        {
            document.GeneratePdf(stream);
        }
        catch (DocumentDrawingException e)
        {
            string? missingGlyphs = MissingGlyphs(e.Message);
            if (missingGlyphs is null)
                throw;

            stream.Dispose();
            // QuestPDF's message holds the whole text, so neither it nor the exception is passed on:
            // only the characters are, because the new message is logged.
            throw new DocumentTextException(missingGlyphs.Length == 0
                ? "The invoice has characters the invoice PDF can't show"
                : $"The invoice has characters the invoice PDF can't show: {missingGlyphs}");
        }
        stream.Position = 0;

        return stream;
    }

    // The rest of the "Missing glyphs:" line of QuestPDF's message; null for any other drawing error
    private static string? MissingGlyphs(string message)
    {
        const string marker = "Missing glyphs:";

        int start = message.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            return null;

        start += marker.Length;
        int end = message.IndexOfAny(['\r', '\n'], start);

        return (end < 0 ? message[start..] : message[start..end]).Trim();
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

    private IDocument GetDocument(Language languageCode, DocumentType documentType, InvoiceModelFull invoiceData, IInvoiceTexts texts)
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
