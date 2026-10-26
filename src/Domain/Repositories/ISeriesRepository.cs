using Domain.Entities;

namespace Domain.Repositories;

public interface ISeriesRepository
{
    Task<SeriesEntity?> Get(Guid id);
    Task<(IEnumerable<SeriesEntity>, int)> Get(Guid? sellerId, int page, int pageSize);
    Task<SeriesEntity?> GetByName(Guid sellerId, string name);
    Task<bool> IsInUse(Guid id);
    Task<Guid> Add(SeriesEntity series);
    Task<(bool Updated, int MaxInvoiceNumber)?> Update(SeriesEntity series);
    Task Delete(Guid id);
}
