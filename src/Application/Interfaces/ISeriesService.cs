using Application.Models;
using BillerContracts;
using BillerContracts.Requests.Series;

namespace Application.Interfaces;

public interface ISeriesService
{
    Task<Guid> Add(SeriesModel series);
    Task Delete(Guid id);
    Task<(IEnumerable<SeriesModel>, int)> Get(SeriesGetRequest? query);
    Task<SeriesModel> Get(Guid id);
    Task<Result<SeriesModel>> GetWithValidation(Guid id, Guid sellerId);
    Task Update(SeriesModel series);
}
