using Application.Models;
using BillerContracts.Requests.Series;

namespace Application.Interfaces;

public interface ISeriesService
{
    Task<Guid> Add(SeriesModel series);
    Task Delete(Guid id);
    Task<(IEnumerable<SeriesModel> Items, int TotalCount, int Page)> Get(SeriesGetRequest? query);
    Task<SeriesModel> Get(Guid id);
    Task<SeriesModel> GetForSeller(Guid id, Guid sellerId);
    Task Update(SeriesModel series);
}
