using Application.Helpers.Invoice;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Application.Models.Invoice;

public class CustomerComponent : IComponent
{
    private readonly CustomerModel _customer;
    private readonly IInvoiceTexts _texts;

    public CustomerComponent(CustomerModel customer, IInvoiceTexts texts)
    {
        _customer = customer;
        _texts = texts;
    }

    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(2);

            column.Item().BorderBottom(1).PaddingBottom(5).Text(_texts.BuyerTitle()).SemiBold();

            column.Item().Text($"{_customer.CompanyName}").Bold();
            column.Item().Text(text =>
            {
                text.Span($"{_texts.CompanyNumber()}: ").Bold();
                text.Span($"{_customer.CompanyNumber}");
            });

            column.Item().Text(text =>
            {
                text.Span($"{_texts.Adress()}: ").Bold();
                text.Span($"{_customer.Street}, {_customer.City}");
            });

            column.Item().Text(text =>
            {
                text.Span($"{_texts.Phone()}.: ").Bold();
                text.Span($"{_customer.Phone}");
            });

            column.Item().Text(text =>
            {
                text.Span($"{_texts.Email()}: ").Bold();
                text.Span($"{_customer.Email}");
            });
        });
    }
}
