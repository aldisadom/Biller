using Domain.Entities;

namespace Domain.Repositories;

public interface ISellerRepository
{
    Task<SellerEntity?> Get(Guid id);
    Task<(IEnumerable<SellerEntity> Items, int TotalCount, int Page)> Get(Guid? userId, int page, int pageSize);
    Task<Guid> Add(SellerEntity seller);
    Task Update(SellerEntity seller);
    Task Delete(Guid id);
}
