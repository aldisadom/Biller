﻿using Application.Models;
using Contracts.Requests.InvoiceClient;
using Contracts.Requests.InvoiceItem;

namespace Application.Interfaces;

public interface IInvoiceItemService
{
    Task<Guid> Add(InvoiceItemModel item);
    Task Delete(Guid id);
    Task<IEnumerable<InvoiceItemModel>> Get(InvoiceItemGetRequest query);
    Task<InvoiceItemModel> Get(Guid id);
    Task Update(InvoiceItemModel item);
}