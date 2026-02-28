using Application.Models.Invoice.Texts;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Application.Models.Invoice;

public class SellerComponent : IComponent
{
    private readonly SellerModel _seller;
    private readonly IInvoiceTexts _texts;

    public SellerComponent(SellerModel seller, IInvoiceTexts texts)
    {
        _seller = seller;
        _texts = texts;
    }

    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(2);

            column.Item().BorderBottom(1).PaddingBottom(5).Text(_texts.SellerTitle()).SemiBold();

            column.Item().Text($"{_seller.CompanyName}").Bold();
            column.Item().Text(text =>
            {
                text.Span($"{_texts.IndividualNumber()}: ").Bold();
                text.Span($"{_seller.CompanyNumber}");
            });

            column.Item().Text(text =>
            {
                text.Span($"{_texts.Adress()}: ").Bold();
                text.Span($"{_seller.Street}, {_seller.City}");
            });

            column.Item().Text(text =>
            {
                text.Span($"{_texts.Phone()}: ").Bold();
                text.Span($"{_seller.Phone}");
            });

            column.Item().Text(text =>
            {
                text.Span($"{_texts.Email()}: ").Bold();
                text.Span($"{_seller.Email}");
            });

            column.Item().Text($"{_seller.BankName}");
            column.Item().Text(text =>
            {
                text.Span($"{_texts.BankAccount()}: ").Bold();
                text.Span($"{_seller.BankNumber}");
            });
        });
    }
}
