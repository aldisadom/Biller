using Application.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services;

public class AddressComponent : IComponent
{
    private string Title { get; }
    private InvoiceAddressModel Address { get; }

    public AddressComponent(string title, InvoiceAddressModel address)
    {
        Title = title;
        Address = address;
    }

    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(2);

            column.Item().BorderBottom(1).PaddingBottom(5).Text(Title).SemiBold();

            column.Item().Text(Address.CompanyName);
            column.Item().Text(Address.Street);
            column.Item().Text($"{Address.City}, {Address.State}");
            column.Item().Text(Address.Email);
            column.Item().Text(Address.Phone);
        });
    }
}

public class InvoiceDocument : IDocument
{
    public InvoiceModel Model { get; }
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    private readonly string _invoiceText = string.Empty;
    private readonly string _issueDateText = string.Empty;
    private readonly string _dueDateText = string.Empty;
    private readonly string _fromText = string.Empty;
    private readonly string _forText = string.Empty;
    private readonly string _grandTotalText = string.Empty;
    private readonly string _nrText = string.Empty;
    private readonly string _productText = string.Empty;
    private readonly string _unitPriceText = string.Empty;
    private readonly string _quantityText = string.Empty;
    private readonly string _totalText = string.Empty;
    private readonly string _currencyText = string.Empty;
    private readonly string _commentText = string.Empty;

    public InvoiceDocument(InvoiceModel model)
    {
        Model = model;
        _invoiceText = "Invoice";
        _issueDateText = "Issue date";
        _dueDateText = "Due date";
        _fromText = "From";
        _forText = "For";
        _grandTotalText = "Grand total";
        _nrText = "Nr";
        _productText = "Product";
        _unitPriceText = "Unit Price";
        _quantityText = "Quantity";
        _totalText = "Total";
        _currencyText = "$";
        _commentText = "Comment";
    }

    void ComposeHeader(IContainer container)
    {
        var titleStyle = TextStyle.Default.FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"{_invoiceText} #{Model.InvoiceNumber}").Style(titleStyle);

                column.Item().Text(text =>
                {
                    text.Span($"{_issueDateText}: ").SemiBold();
                    text.Span($"{Model.IssueDate:d}");
                });

                column.Item().Text(text =>
                {
                    text.Span($"{_dueDateText}: ").SemiBold();
                    text.Span($"{Model.DueDate:d}");
                });
            });

            row.ConstantItem(100).Height(50).Placeholder();
        });
    }

    void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            // step 1
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(25);
                columns.RelativeColumn(3);
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            // step 2
            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text(_nrText);
                header.Cell().Element(CellStyle).Text(_productText);
                header.Cell().Element(CellStyle).AlignRight().Text(_unitPriceText);
                header.Cell().Element(CellStyle).AlignRight().Text(_quantityText);
                header.Cell().Element(CellStyle).AlignRight().Text(_totalText);

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                }
            });

            // step 3
            foreach (InvoiceItemModel item in Model.Items)
            {
                table.Cell().Element(CellStyle).Text((Model.Items.IndexOf(item) + 1).ToString());
                table.Cell().Element(CellStyle).Text(item.Name);
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Price}{_currencyText}");
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Price}");
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Price}{_currencyText}");

                static IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            }
        });
    }
    void ComposeComments(IContainer container)
    {
        if (Model.Comments is null || Model.Comments == string.Empty)
            return;

        container.Background(Colors.Grey.Lighten3).Padding(10).Column(column =>
        {
            column.Spacing(5);
            column.Item().Text(_commentText).FontSize(14);
            column.Item().Text(Model.Comments);
        });
    }

    void ComposeContent(IContainer container)
    {
        container.PaddingVertical(40).Column(column =>
        {
            column.Spacing(5);

            column.Item().Row(row =>
            {
                row.RelativeItem().Component(new AddressComponent(_fromText, Model.SellerAddress));
                row.ConstantItem(50);
                row.RelativeItem().Component(new AddressComponent(_forText, Model.CustomerAddress));
            });

            column.Item().Element(ComposeTable);

            var totalPrice = 10.0m;
            column.Item().AlignRight().Text($"{_grandTotalText}: {totalPrice}$").FontSize(14);

            if (!string.IsNullOrWhiteSpace(Model.Comments))
                column.Item().PaddingTop(25).Element(ComposeComments);
        });
    }

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Margin(50);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);


                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
    }
}
