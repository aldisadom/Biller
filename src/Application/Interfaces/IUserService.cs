using Application.Models;
using BillerContracts.Requests.User;

namespace Application.Interfaces;

public interface IUserService
{
    Task<string> Login(UserModel user);
    Task<Guid> Add(UserModel user);
    Task Delete(Guid id);
    Task<(IEnumerable<UserModel> Items, int TotalCount, int Page)> Get(UserGetRequest? query);
    Task<UserModel> Get(Guid id);
    Task Update(UserModel user);
}
