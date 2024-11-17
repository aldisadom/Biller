using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Common;
using Contracts.Requests.Seller;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using System.Net;

namespace Application.Services;

public class SellerService : ISellerService
{
    private readonly ISellerRepository _sellerRepository;
    private readonly IMapper _mapper;

    public SellerService(ISellerRepository sellerRepository, IMapper mapper)
    {
        _sellerRepository = sellerRepository;
        _mapper = mapper;
    }

    public async Task<SellerModel> Get(Guid id)
    {
        SellerEntity sellerEntity = await _sellerRepository.Get(id)
            ?? throw new NotFoundException($"Invoice seller:{id} not found");

        return _mapper.Map<SellerModel>(sellerEntity);
    }

    public async Task<IEnumerable<SellerModel>> Get(SellerGetRequest? query)
    {
        IEnumerable<SellerEntity> sellerEntities;

        if (query is null)
            sellerEntities = await _sellerRepository.Get();
        else if (query.UserId is not null)
            sellerEntities = await _sellerRepository.GetByUserId((Guid)query.UserId);
        else
            sellerEntities = await _sellerRepository.Get();

        return _mapper.Map<IEnumerable<SellerModel>>(sellerEntities);
    }

    public async Task<Result<SellerModel>> GetWithValidation(Guid id, Guid userId)
    {
        var seller = await _sellerRepository.Get(id);
        
        if (seller is null || seller.UserId != userId)
            return new ErrorModel() { StatusCode = HttpStatusCode.BadRequest, Message = "Validation failure", ExtendedMessage = $"Seller id {id} is invalid for user id {userId}" };

        return _mapper.Map<SellerModel>(seller);
    }

    public async Task<Guid> Add(SellerModel seller)
    {
        SellerEntity sellerEntity = _mapper.Map<SellerEntity>(seller);

        return await _sellerRepository.Add(sellerEntity);
    }

    public async Task Update(SellerModel seller)
    {
        await Get(seller.Id);

        SellerEntity sellerEntity = _mapper.Map<SellerEntity>(seller);

        await _sellerRepository.Update(sellerEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _sellerRepository.Delete(id);
    }
}
