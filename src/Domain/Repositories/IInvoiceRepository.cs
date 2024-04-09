using Domain.Entities;

namespace Domain.Repositories
{
    public interface IInvoiceRepository
    {
        Task<Guid> Add(InvoiceDataEntity invoice);
        Task Delete(Guid id);
        Task<IEnumerable<InvoiceDataEntity>> Get();
        Task<InvoiceDataEntity?> Get(Guid id);
        Task<IEnumerable<InvoiceDataEntity>> Get(List<Guid> ids);
        Task<IEnumerable<InvoiceDataEntity>> GetByCustomerId(Guid customerId);
        Task Update(InvoiceDataEntity invoice);
    }
}
