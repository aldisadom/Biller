using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Application.Models.InvoiceGenerationModels;

public class CustomerComponent : IComponent
{
    private readonly string _title;
    private readonly CustomerModel _customer;

    public CustomerComponent(string title, CustomerModel customer)
    {
        _title = title;
        _customer = customer;
    }

    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(2);

            column.Item().BorderBottom(1).PaddingBottom(5).Text(_title).SemiBold();

            column.Item().Text($"{_customer.CompanyName}").Bold();
            column.Item().Text(text =>
            {
                text.Span($"Juridinio asmens kodas: ").Bold();
                text.Span($"{_customer.CompanyNumber}");
            });

            column.Item().Text(text =>
            {
                text.Span($"Adresas: ").Bold();
                text.Span($"{_customer.Street}, {_customer.City}");
            });

            column.Item().Text(text =>
            {
                text.Span($"Tel.: ").Bold();
                text.Span($"{_customer.Phone}");
            });

            column.Item().Text(text =>
            {
                text.Span($"El. paštas: ").Bold();
                text.Span($"{_customer.Email}");
            });
        });
    }
}
