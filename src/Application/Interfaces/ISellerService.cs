using Application.Models;
using BillerContracts.Requests.Seller;

namespace Application.Interfaces
{
    public interface ISellerService
    {
        Task<Guid> Add(SellerModel seller);
        Task Delete(Guid id);
        Task<SellerModel> Get(Guid id);
        Task<SellerModel> GetForUser(Guid id, Guid userId);
        Task<(IEnumerable<SellerModel> Items, int TotalCount, int Page)> Get(SellerGetRequest? query);
        Task Update(SellerModel seller);
    }
}
