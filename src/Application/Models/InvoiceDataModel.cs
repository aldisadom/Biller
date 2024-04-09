﻿namespace Application.Models;

public class InvoiceDataModel
{
    public Guid CustomerId { get; set; }
    public Guid SellerId { get; set; }
    public Guid UserId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserModel? User { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime DueDate { get; set; }
    public SellerModel? Seller { get; set; }
    public CustomerModel? Customer { get; set; }
    public List<InvoiceItemModel>? Items { get; set; }
    public string? Comments { get; set; }
    public decimal TotalPrice { get; set; }
}
