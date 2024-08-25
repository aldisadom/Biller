using Application.Models;
using Contracts.Requests.Seller;

namespace Application.Interfaces
{
    public interface ISellerService
    {
        Task<Guid> Add(SellerModel seller);
        Task Delete(Guid id);
        Task<SellerModel> Get(Guid id);
        Task<IEnumerable<SellerModel>> Get(SellerGetRequest? query);
        Task Update(SellerModel seller);
    }
}
