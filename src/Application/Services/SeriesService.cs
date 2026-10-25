using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using BillerContracts;
using BillerContracts.Enums;
using BillerContracts.Requests.Series;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentValidation;
using System.Net;

namespace Application.Services;

public class SeriesService : ISeriesService
{
    private readonly ISeriesRepository _seriesRepository;
    private readonly ISellerService _sellerService;

    public SeriesService(ISeriesRepository seriesRepository, ISellerService sellerService)
    {
        _seriesRepository = seriesRepository;
        _sellerService = sellerService;
    }

    public async Task<SeriesModel> Get(Guid id)
    {
        SeriesEntity seriesEntity = await _seriesRepository.Get(id)
            ?? throw new NotFoundException($"Invoice series:{id} not found");

        return seriesEntity.ToModel();
    }

    public async Task<(IEnumerable<SeriesModel>, int)> Get(SeriesGetRequest? query)
    {
        int page = query?.Page ?? 1;
        var pageSize = query?.PageSize ?? PageSize.p25;

        var (entities, totalCount) = await _seriesRepository.Get(query?.SellerId, page, (int)pageSize);

        return (entities.Select(s => s.ToModel()), totalCount);
    }

    public async Task<Result<SeriesModel>> GetWithValidation(Guid id, Guid sellerId)
    {
        var series = await _seriesRepository.Get(id);

        if (series is null || series.SellerId != sellerId)
            return new ErrorModel() { StatusCode = HttpStatusCode.BadRequest, Message = "Validation failure", ExtendedMessage = $"Series id {id} is invalid for seller id {sellerId}" };

        return series.ToModel();
    }

    public async Task<Guid> Add(SeriesModel series)
    {
        await _sellerService.Get(series.SellerId);
        await CheckNameIsFree(series);

        SeriesEntity seriesEntity = series.ToEntity();

        return await _seriesRepository.Add(seriesEntity);
    }

    public async Task Update(SeriesModel series)
    {
        SeriesModel stored = await Get(series.Id);
        series.SellerId = stored.SellerId;
        await CheckNameIsFree(series);

        SeriesEntity seriesEntity = series.ToEntity();

        if (await _seriesRepository.Update(seriesEntity, stored.NextNumber))
            return;

        await Get(series.Id);

        int maxInvoiceNumber = await _seriesRepository.GetMaxInvoiceNumber(series.Id);
        if (series.NextNumber <= maxInvoiceNumber)
            throw new ValidationException($"Next number must be greater than {maxInvoiceNumber}, the highest invoice number in series {stored.Name}");

        throw new ValidationException($"Series {stored.Name} got a new or deleted invoice while it was updated, please try again");
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        if (await _seriesRepository.IsInUse(id))
            throw new ValidationException($"Series id {id} is used by customers or invoices");

        await _seriesRepository.Delete(id);
    }

    private async Task CheckNameIsFree(SeriesModel series)
    {
        SeriesEntity? sameName = await _seriesRepository.GetByName(series.SellerId, series.Name);

        if (sameName is not null && sameName.Id != series.Id)
            throw new ValidationException($"Series {series.Name} already exists for seller id {series.SellerId}");
    }
}
