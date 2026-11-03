using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using BillerContracts;
using BillerContracts.Enums;
using BillerContracts.Requests.Seller;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using System.Net;

namespace Application.Services;

public class SellerService : ISellerService
{
    private readonly ISellerRepository _sellerRepository;

    public SellerService(ISellerRepository sellerRepository)
    {
        _sellerRepository = sellerRepository;
    }

    public async Task<SellerModel> Get(Guid id)
    {
        SellerEntity sellerEntity = await _sellerRepository.Get(id)
            ?? throw new NotFoundException($"Invoice seller:{id} not found");

        return sellerEntity.ToModel();
    }

    public async Task<(IEnumerable<SellerModel>, int)> Get(SellerGetRequest? query)
    {
        int page = query?.Page ?? 1;
        var pageSize = query?.PageSize ?? PageSize.p25;

        var (entities, totalCount) = await _sellerRepository.Get(query?.UserId, page, (int)pageSize);

        return (entities.Select(s => s.ToModel()), totalCount);
    }

    public async Task<Result<SellerModel>> GetWithValidation(Guid id, Guid userId)
    {
        var seller = await _sellerRepository.Get(id);

        if (seller is null || seller.UserId != userId)
            return new ErrorModel() { StatusCode = HttpStatusCode.BadRequest, Message = "Validation failure", ExtendedMessage = $"Seller id {id} is invalid for user id {userId}" };

        return seller.ToModel();
    }

    public async Task<Guid> Add(SellerModel seller)
    {
        SellerEntity sellerEntity = seller.ToEntity();

        return await _sellerRepository.Add(sellerEntity);
    }

    public async Task Update(SellerModel seller)
    {
        await Get(seller.Id);

        SellerEntity sellerEntity = seller.ToEntity();

        await _sellerRepository.Update(sellerEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _sellerRepository.Delete(id);
    }
}
