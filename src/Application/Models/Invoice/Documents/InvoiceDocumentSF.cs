using Application.Helpers.Invoice;
using Application.Helpers.PriceToWords;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Application.Models.Invoice.Documents;

public class InvoiceDocumentSF : IDocument
{
    public InvoiceModel Model { get; }
    private readonly IPriceToWords _priceToWords;
    private readonly IInvoiceTexts _texts;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public InvoiceDocumentSF(InvoiceModel model, IPriceToWords priceToWords, IInvoiceTexts texts)
    {
        Model = model;
        _priceToWords = priceToWords;
        _texts = texts;
    }

    private void ComposeHeader(IContainer container)
    {
        var titleStyle = TextStyle.Default.FontSize(20).SemiBold();

        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().AlignCenter().Text($"{_texts.Invoice()}").Style(titleStyle);
                column.Item().AlignCenter().Text($"{_texts.InvoiceSeries()} {Model.Customer!.InvoiceName} {_texts.NumberShort()} {Model.GenerateInvoiceName()}");
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(x =>
            {
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        });
    }

    private void ComposeInvoiceDetails(IContainer container)
    {
        container.ShowEntire().PaddingTop(5).Column(column =>
        {
            column.Item().Text(text =>
            {
                text.Span($"{_texts.CreationDate()}: ").SemiBold();
                text.Span($"{Model.CreatedDate:yyyy-MM-dd}");
            });

            column.Item().Text(text =>
            {
                text.Span($"{_texts.DueDate()}: ").SemiBold();
                text.Span($"{Model.DueDate:yyyy-MM-dd}");
            });

            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Component(new SellerComponent(Model.Seller!, _texts));
                row.ConstantItem(50);
                row.RelativeItem().Component(new CustomerComponent(Model.Customer!, _texts));
            });
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.PaddingTop(30).Table(table =>
        {
            // step 1
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(25);

                columns.RelativeColumn(20);
                columns.RelativeColumn(5);
                columns.RelativeColumn(3);
                columns.RelativeColumn(6);
            });

            // step 2
            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text(_texts.NumberShort());
                header.Cell().Element(CellStyle).Text(_texts.ItemName());
                header.Cell().Element(CellStyle).AlignRight().Text(_texts.ItemPrice());
                header.Cell().Element(CellStyle).AlignRight().Text(_texts.Amount());
                header.Cell().Element(CellStyle).AlignRight().Text(_texts.Sum());

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.SemiBold())
                                                            .Border(1)
                                                            .BorderColor(Colors.Grey.Medium)
                                                            .PaddingVertical(1)
                                                            .PaddingHorizontal(1);
                }
            });

            // step 3
            foreach (InvoiceItemModel item in Model.Items!)
            {
                table.Cell().Element(CellStyle).Text((Model.Items.IndexOf(item) + 1).ToString());
                if (item.Comments is null || item.Comments == string.Empty)
                    table.Cell().Element(CellStyle).Text(item.Name);
                else
                    table.Cell().Element(CellStyle).Text($"{item.Name} ({item.Comments})");

                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Price}");
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Quantity}");
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.CalculateTotal():0.##}");

                static IContainer CellStyle(IContainer container)
                {
                    return container.Border(1)
                                    .BorderColor(Colors.Grey.Medium)
                                    .PaddingVertical(5)
                                    .PaddingHorizontal(2);
                }
            }
        });
    }

    private void ComposeGrandTotal(IContainer container)
    {
        container.ShowEntire().PaddingTop(5).Column(column =>
        {
            column.Item().AlignRight().Text($"{_texts.SumTotal()}: {Model.CalculateTotal():0.##}{_texts.Currency()}").FontSize(14);
            column.Spacing(5);
            column.Item().Text($"{_texts.SumInWords()}: {_priceToWords.Decode(Model.CalculateTotal())}");
        });
    }

    private void ComposeComments(IContainer container)
    {
        if (string.IsNullOrWhiteSpace(Model.Comments))
            return;

        container.ShowEntire().Background(Colors.Grey.Lighten3).Padding(20).Column(column =>
        {
            column.Item().Text(_texts.Comment()).FontSize(14);
            column.Item().Text(Model.Comments);
        });
    }

    private void ComposeReciever(IContainer container)
    {
        container.ShowEntire().PaddingTop(10).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(110).Text($"{_texts.InvoicedBy()}: ");
                row.RelativeItem().BorderBottom(1).Text($"{Model.User!.Name} {Model.User.LastName}");
            });

            column.Item().Row(row =>
            {
                row.ConstantItem(110).Text($"{_texts.ServiceRecieved()}: ");
                row.RelativeItem().BorderBottom(1);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(30).Column(column =>
        {
            column.Spacing(5);

            column.Item().Element(ComposeInvoiceDetails);
            column.Item().Element(ComposeTable);
            column.Item().Element(ComposeGrandTotal);
            column.Item().Element(ComposeReciever);

            column.Item().Element(ComposeComments);

        });
    }

    public void Compose(IDocumentContainer container)
    {

        container
            .Page(page =>
            {
                page.DefaultTextStyle(x => x.FontFamily("calibri"));
                page.Margin(50);

                page.Header().Element(ComposeHeader);

                page.Content().Element(ComposeContent);

                page.Footer().Element(ComposeFooter);
            });
    }
}
