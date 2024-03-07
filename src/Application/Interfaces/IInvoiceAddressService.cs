using Application.Models;
using Contracts.Requests.InvoiceAddress;

namespace Application.Interfaces;

public interface IInvoiceAddressService
{
    Task<Guid> Add(InvoiceAddressModel invoiceAddress);
    Task Delete(Guid id);
    Task<IEnumerable<InvoiceAddressModel>> Get(InvoiceAddressGetRequest query);
    Task<InvoiceAddressModel> Get(Guid id);
    Task Update(InvoiceAddressModel invoiceAddress);
}