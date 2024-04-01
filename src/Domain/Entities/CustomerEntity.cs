﻿namespace Domain.Entities;

public record CustomerEntity
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public string InvoiceName { get; set; } = string.Empty;
    public int InvoiceNumber { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyNumber { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public object Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
