using Domain.Entities;

namespace Domain.Repositories;

public interface ISellerRepository
{
    Task<SellerEntity?> Get(Guid id);
    Task<IEnumerable<SellerEntity>> GetByUserId(Guid userId);
    Task<IEnumerable<SellerEntity>> Get();
    Task<Guid> Add(SellerEntity seller);
    Task Update(SellerEntity seller);
    Task Delete(Guid id);
}
