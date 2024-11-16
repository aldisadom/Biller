using Application.Models;
using Common;
using Contracts.Requests.Seller;

namespace Application.Interfaces
{
    public interface ISellerService
    {
        Task<Guid> Add(SellerModel seller);
        Task Delete(Guid id);
        Task<SellerModel> Get(Guid id);
        Task<Result<SellerModel>> GetWithValidation(Guid id, Guid userId);
        Task<IEnumerable<SellerModel>> Get(SellerGetRequest? query);
        Task Update(SellerModel seller);
    }
}
