using Domain.Entities;

namespace Domain.Repositories;

public interface IUserRepository
{
    Task<UserEntity?> Get(Guid id);
    Task<IEnumerable<UserEntity>> Get();
    Task<Guid> Add(UserEntity user);
    Task Update(UserEntity user);
    Task Delete(Guid id);
}
