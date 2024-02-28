using Application.Models;

namespace Application.Interfaces;

public interface IUserService
{
    Task<string> Login(UserModel user);
    Task<Guid> Add(UserModel user);
    Task Delete(Guid id);
    Task<IEnumerable<UserModel>> Get();
    Task<UserModel> Get(Guid id);
    Task Update(UserModel user);
}