using BillerContracts.Enums;
using Domain.Entities;

namespace Domain.Repositories
{
    public interface IInvoiceRepository
    {
        Task<Guid> Add(InvoiceEntity invoice);
        Task Delete(Guid id);
        Task<(IEnumerable<InvoiceEntity>, int)> Get(Guid? userId, Guid? sellerId, Guid? customerId, int page, int pageSize);
        Task<InvoiceEntity?> Get(Guid id);
        Task Update(InvoiceEntity invoice);
        Task UpdateStatus(Guid id, InvoiceStatus status);
    }
}
