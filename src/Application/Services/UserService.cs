using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Enums;
using BillerContracts.Requests.User;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordEncryptionService _passwordEncryptionService;

    public UserService(IUserRepository userRepository, IPasswordEncryptionService passwordEncryptionService)
    {
        _userRepository = userRepository;
        _passwordEncryptionService = passwordEncryptionService;
    }

    public async Task<string> Login(UserModel user)
    {
        UserEntity userEntity = await _userRepository.Get(user.Email)
            ?? throw new UnauthorizedAccessException($"Invalid login data");

        (string hashed, _) = _passwordEncryptionService.Encrypt(user.Password, userEntity.Salt);

        if (hashed != userEntity.Password)
            throw new UnauthorizedAccessException($"Invalid login data");

        return "fakeToken";
    }

    public async Task<UserModel> Get(Guid id)
    {
        UserEntity userEntity = await _userRepository.Get(id)
            ?? throw new NotFoundException($"User:{id} not found");

        return userEntity.ToModel();
    }

    public async Task<(IEnumerable<UserModel>, int)> Get(UserGetRequest? query)
    {
        int page = query?.Page ?? 1;
        var pageSize = query?.PageSize ?? PageSize.p25;

        var (entities, totalCount) = await _userRepository.Get(page, (int)pageSize);

        return (entities.Select(u => u.ToModel()), totalCount);
    }

    public async Task<Guid> Add(UserModel user)
    {
        (user.Password, string salt) = _passwordEncryptionService.Encrypt(user.Password);

        UserEntity userEntity = user.ToEntity();
        userEntity.Salt = salt;

        return await _userRepository.Add(userEntity);
    }

    public async Task Update(UserModel user)
    {
        await Get(user.Id);

        UserEntity userEntity = user.ToEntity();

        await _userRepository.Update(userEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _userRepository.Delete(id);
    }
}
