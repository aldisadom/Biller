using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Application.Models.InvoiceGenerationModels;

public class SellerComponent : IComponent
{
    private readonly string _title;
    private readonly SellerModel _seller;

    public SellerComponent(string title, SellerModel seller)
    {
        _title = title;
        _seller = seller;
    }

    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(2);

            column.Item().BorderBottom(1).PaddingBottom(5).Text(_title).SemiBold();

            column.Item().Text($"{_seller.CompanyName}").Bold();
            column.Item().Text(text =>
            {
                text.Span($"Ind. veiklos pažymos Nr.: ").Bold();
                text.Span($"{_seller.CompanyNumber}");
            });

            column.Item().Text(text =>
            {
                text.Span($"Adresas: ").Bold();
                text.Span($"{_seller.Street}, {_seller.City}");
            });

            column.Item().Text(text =>
            {
                text.Span($"Tel.: ").Bold();
                text.Span($"{_seller.Phone}");
            });

            column.Item().Text(text =>
            {
                text.Span($"El. paštas: ").Bold();
                text.Span($"{_seller.Email}");
            });

            column.Item().Text($"{_seller.BankName}");
            column.Item().Text(text =>
            {
                text.Span($"A.s: ").Bold();
                text.Span($"{_seller.BankNumber}");
            });
        });
    }
}
