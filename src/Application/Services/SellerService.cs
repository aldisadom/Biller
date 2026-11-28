using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Enums;
using BillerContracts.Requests.Seller;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentValidation;

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

    public async Task<(IEnumerable<SellerModel> Sellers, int TotalCount, int Page)> Get(SellerGetRequest? query)
    {
        int page = query?.Page ?? 1;
        var pageSize = query?.PageSize ?? PageSize.p25;

        var (sellerEntities, totalCount, returnedPage) = await _sellerRepository.Get(query?.UserId, page, (int)pageSize);

        return (sellerEntities.Select(s => s.ToModel()), totalCount, returnedPage);
    }

    public async Task<SellerModel> GetForUser(Guid id, Guid userId)
    {
        SellerEntity? seller = await _sellerRepository.Get(id);

        if (seller is null || seller.UserId != userId)
            throw new ValidationException($"Seller id {id} is invalid for user id {userId}");

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
