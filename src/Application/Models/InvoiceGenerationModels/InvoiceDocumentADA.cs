using Application.Helpers.PriceToWords;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Application.Models.InvoiceGenerationModels;

public class InvoiceDocumentADA : IDocument
{
    public InvoiceModel Model { get; }
    private readonly IPriceToWords _priceToWords;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public InvoiceDocumentADA(InvoiceModel model, IPriceToWords priceToWords)
    {
        Model = model;
        _priceToWords = priceToWords;
    }

    private void ComposeHeader(IContainer container)
    {
        var titleStyle = TextStyle.Default.FontSize(20).SemiBold();

        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().AlignCenter().Text($"ATLIKTŲ DARBŲ AKTAS").Style(titleStyle);
                column.Item().AlignCenter().Text($"Serija {Model.Customer!.InvoiceName} Nr. {Model.GenerateInvoiceName()}");
            });
            //place for image
            //            row.ConstantItem(100).Height(50).Placeholder();
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
                text.Span($"Išrašymo data: ").SemiBold();
                text.Span($"{Model.CreatedDate:yyyy-MM-dd}");
            });

            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Component(new SellerComponent("Prekių / paslaugų pardavėjas", Model.Seller!));
                row.ConstantItem(50);
                row.RelativeItem().Component(new CustomerComponent("Prekių / paslaugų pirkėjas", Model.Customer!));
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
                header.Cell().Element(CellStyle).Text("Nr.");
                header.Cell().Element(CellStyle).Text("Prekės, turto ar paslaugos pavadinimas");
                header.Cell().Element(CellStyle).AlignRight().Text("vnt. kaina, €");
                header.Cell().Element(CellStyle).AlignRight().Text("Kiekis");
                header.Cell().Element(CellStyle).AlignRight().Text("Suma, €");

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
            column.Item().AlignRight().Text($"Bendra suma: {Model.CalculateTotal():0.##}€").FontSize(14);
            column.Spacing(5);
            column.Item().Text($"Suma žodžiais: {_priceToWords.Decode(Model.CalculateTotal())}");
        });
    }

    private void ComposeComments(IContainer container)
    {
        if (string.IsNullOrWhiteSpace(Model.Comments))
            return;

        container.ShowEntire().Background(Colors.Grey.Lighten3).Padding(20).Column(column =>
        {
            column.Item().Text("Komentaras").FontSize(14);
            column.Item().Text(Model.Comments);
        });
    }

    private void ComposeReciever(IContainer container)
    {
        container.ShowEntire().PaddingTop(10).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(110).Text("Parengė: ");
                row.RelativeItem().BorderBottom(1).Text($"{Model.User!.Name} {Model.User.LastName}");
                row.ConstantItem(70).BorderBottom(1).Text($"{Model.CreatedDate:yyyy-MM-dd}");
            });
            column.Item().Row(row =>
            {
                row.ConstantItem(110);
                row.RelativeItem().AlignCenter()
                                    .Text($"Rangovas: Vardas, Pavardė, Parašas, Data")
                                    .FontSize(8);
            });

            column.Item().Row(row =>
            {
                row.ConstantItem(110).Text("Suderinta: ");
                row.RelativeItem().BorderBottom(1);
            });
            column.Item().Row(row =>
            {
                row.ConstantItem(110);
                row.RelativeItem().AlignCenter()
                                    .Text($"Atsakingas darbuotojas: Pareigos, Vardas, Pavardė, Parašas, Data")
                                    .FontSize(8);
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
