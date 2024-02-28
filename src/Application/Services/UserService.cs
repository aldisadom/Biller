using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Microsoft.Extensions.Configuration;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly string _passwordSalt;

    public UserService(IUserRepository userRepository, IConfiguration configuration, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _passwordSalt = configuration.GetValue<string>("EncryptionSalt")
            ?? throw new ArgumentNullException($"Password salt is missing");
    }

    public async Task<string> Login(UserModel user)
    {
        UserEntity userEntity = await _userRepository.Get(user.Email)
            ?? throw new UnauthorizedAccessException($"Invalid login data");

        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password, _passwordSalt);

        if (user.Password != userEntity.Password)
            throw new UnauthorizedAccessException($"Invalid login data");

        return "fakeToken";
    }

    public async Task<UserModel> Get(Guid id)
    {
        UserEntity userEntity = await _userRepository.Get(id)
            ?? throw new NotFoundException($"User:{id} not found");

        return _mapper.Map<UserModel>(userEntity);
    }

    public async Task<IEnumerable<UserModel>> Get()
    {
        IEnumerable<UserEntity> userEntities = await _userRepository.Get();

        return _mapper.Map<IEnumerable<UserModel>>(userEntities);
    }

    public async Task<Guid> Add(UserModel user)
    {
        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password, _passwordSalt);

        UserEntity userEntity = _mapper.Map<UserEntity>(user);

        return await _userRepository.Add(userEntity);
    }

    public async Task Update(UserModel user)
    {
        await Get(user.Id);

        UserEntity userEntity = _mapper.Map<UserEntity>(user);

        await _userRepository.Update(userEntity);
    }

    public async Task Delete(Guid id)
    {
        await Get(id);

        await _userRepository.Delete(id);
    }
}
