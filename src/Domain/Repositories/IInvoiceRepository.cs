using Domain.Entities;

namespace Domain.Repositories
{
    public interface IInvoiceRepository
    {
        Task<Guid> Add(InvoiceEntity invoice);
        Task Delete(Guid id);
        Task<IEnumerable<InvoiceEntity>> Get();
        Task<InvoiceEntity?> Get(Guid id);
        Task<IEnumerable<InvoiceEntity>> GetByUserId(Guid userId);
        Task<IEnumerable<InvoiceEntity>> GetBySellerId(Guid sellerId);
        Task<IEnumerable<InvoiceEntity>> GetByCustomerId(Guid customerId);
        Task Update(InvoiceEntity invoice);
    }
}
