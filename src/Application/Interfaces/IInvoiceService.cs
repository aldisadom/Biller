﻿using Application.Models;
using Contracts.Requests.InvoiceData;
using Contracts.Requests.Item;
using Domain.Entities;

namespace Application.Interfaces;

public interface IInvoiceService
{
    Task<Guid> Add(InvoiceDataModel invoiceData);
    Task Delete(Guid id);
    Task<IEnumerable<InvoiceDataModel>> Get(InvoiceDataGetRequest query);
    Task<InvoiceDataModel> Get(Guid id);
    Task Update(InvoiceDataModel invoiceData);
    Task GeneratePDF(Guid id);
}
