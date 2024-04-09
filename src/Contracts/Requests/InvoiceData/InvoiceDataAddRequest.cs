﻿using System.ComponentModel.DataAnnotations;

namespace Contracts.Requests.InvoiceData;

public class InvoiceDataAddRequest
{
    [Required]
    public Guid SellerId { get; set; }
    [Required]
    public Guid CustomerId { get; set; }
    [Required]
    public Guid UserId { get; set; }
    [Required]
    [MinLength(1)]
    public List<InvoiceItemRequest> Items { get; set; } = new();
    public string Comments { get; set; } = string.Empty;
    [Required]
    public DateTime DueDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
