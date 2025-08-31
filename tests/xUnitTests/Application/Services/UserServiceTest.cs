using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoFixture.Xunit2;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using WebAPI.MappingProfiles;

namespace xUnitTests.Application.Services;

public class UserServiceTest
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordEncryptionService> _passwordEncryptionServiceMock;
    private readonly UserService _userService;
    private readonly IMapper _mapper;

    public UserServiceTest()
    {
        _userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);

        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new UserMappingProfile());
        });
        mapperConfig.AssertConfigurationIsValid();
        _mapper = mapperConfig.CreateMapper();

        _passwordEncryptionServiceMock = new Mock<IPasswordEncryptionService>(MockBehavior.Strict);
        _userService = new UserService(_userRepositoryMock.Object, _passwordEncryptionServiceMock.Object, _mapper);
    }

    [Theory]
    [AutoData]
    public async Task Login_GivenValidPassword_ReturnsToken(UserEntity user)
    {
        //Arrange
        string expectedResult = "fakeToken";

        _userRepositoryMock.Setup(m => m.Get(user.Email))
                        .ReturnsAsync(user);

        _passwordEncryptionServiceMock.Setup(m => m.Encrypt(user.Password, It.IsAny<string>()))
                        .Returns((user.Password, " "));

        UserModel userModel = _mapper.Map<UserModel>(user);

        //Act
        string result = await _userService.Login(userModel);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _userRepositoryMock.Verify(m => m.Get(user.Email), Times.Once());
        _passwordEncryptionServiceMock.Verify(m => m.Encrypt(user.Password, It.IsAny<string>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Login_GivenInvalidUser_ThrowsUnauthorizedException(UserEntity user)
    {
        //Arrange
        _userRepositoryMock.Setup(m => m.Get(user.Email))
                        .ReturnsAsync((UserEntity)null!);

        UserModel userModel = _mapper.Map<UserModel>(user);

        //Act
        //Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _userService.Login(userModel));

        _userRepositoryMock.Verify(m => m.Get(user.Email), Times.Once());
        _passwordEncryptionServiceMock.Verify(m => m.Encrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Login_GivenInvalidPassword_ThrowsUnauthorizedException(UserEntity user)
    {
        //Arrange
        _userRepositoryMock.Setup(m => m.Get(user.Email))
                        .ReturnsAsync(user);

        _passwordEncryptionServiceMock.Setup(m => m.Encrypt(user.Password, It.IsAny<string>()))
                        .Returns(("a", " "));

        UserModel userModel = _mapper.Map<UserModel>(user);

        //Act
        //Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _userService.Login(userModel));

        _userRepositoryMock.Verify(m => m.Get(user.Email), Times.Once());
        _passwordEncryptionServiceMock.Verify(m => m.Encrypt(user.Password, It.IsAny<string>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenValidId_ReturnsDTO(UserEntity user)
    {
        //Arrange
        _userRepositoryMock.Setup(m => m.Get(user.Id))
                        .ReturnsAsync(user);

        UserModel expectedResult = _mapper.Map<UserModel>(user);

        //Act
        UserModel result = await _userService.Get(user.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _userRepositoryMock.Verify(m => m.Get(user.Id), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException(Guid id)
    {
        // Arrange
        _userRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((UserEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _userService.Get(id));

        _userRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Get_ReturnsDTO(List<UserEntity> userList)
    {
        //Arrange
        _userRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(userList);

        List<UserModel> expectedResult = _mapper.Map<List<UserModel>>(userList);

        //Act
        var result = await _userService.Get();

        //Assert
        result.Count().Should().Be(userList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _userRepositoryMock.Verify(m => m.Get(), Times.Once());
    }


    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        List<UserEntity> userList = [];

        //Arrange
        _userRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(userList);

        // Act Assert
        var result = await _userService.Get();

        result.Count().Should().Be(0);
        result.Should().BeEquivalentTo(new List<UserModel>());

        _userRepositoryMock.Verify(m => m.Get(), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Add_GivenValidId_ReturnsGuid(UserModel user)
    {
        //Arrange
        UserEntity userEntity = _mapper.Map<UserEntity>(user);
        userEntity.Salt = " ";
        _userRepositoryMock.Setup(m => m.Add(It.Is<UserEntity>
                                (x => x == userEntity)))
                                 .ReturnsAsync(user.Id);

        _passwordEncryptionServiceMock.Setup(m => m.Encrypt(user.Password, null))
                                .Returns((user.Password, " "));

        //Act
        Guid result = await _userService.Add(user);

        //Assert
        result.Should().Be(user.Id);

        _userRepositoryMock.Verify(m => m.Add(userEntity), Times.Once());
        _passwordEncryptionServiceMock.Verify(m => m.Encrypt(user.Password, null), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_ReturnsSuccess(UserModel user)
    {
        //Arrange
        UserEntity userEntity = _mapper.Map<UserEntity>(user);

        _userRepositoryMock.Setup(m => m.Update(It.Is<UserEntity>(x => x == userEntity)))
                        .Returns(Task.CompletedTask);

        _userRepositoryMock.Setup(m => m.Get(userEntity.Id))
                                .ReturnsAsync(userEntity);

        //Act
        //Assert
        await _userService.Invoking(x => x.Update(user))
                                        .Should().NotThrowAsync<Exception>();

        _userRepositoryMock.Verify(m => m.Get(user.Id), Times.Once());
        _userRepositoryMock.Verify(m => m.Update(userEntity), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_InvalidId_NotFoundException(UserModel user)
    {
        //Arrange
        UserEntity userEntity = _mapper.Map<UserEntity>(user);

        _userRepositoryMock.Setup(m => m.Update(It.Is<UserEntity>
                                (x => x == userEntity)));

        _userRepositoryMock.Setup(m => m.Get(user.Id))
                        .ReturnsAsync((UserEntity)null!);

        //Act
        //Assert
        await _userService.Invoking(x => x.Update(user))
                            .Should().ThrowAsync<NotFoundException>();

        _userRepositoryMock.Verify(m => m.Get(user.Id), Times.Once());
        _userRepositoryMock.Verify(m => m.Update(It.IsAny<UserEntity>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Delete_ValidId(UserEntity user)
    {
        //Arrange
        _userRepositoryMock.Setup(m => m.Delete(user.Id))
                        .Returns(Task.CompletedTask);

        _userRepositoryMock.Setup(m => m.Get(user.Id))
                        .ReturnsAsync(user);

        //Act
        //Assert
        await _userService.Invoking(x => x.Delete(user.Id))
                            .Should().NotThrowAsync<Exception>();

        _userRepositoryMock.Verify(m => m.Get(user.Id), Times.Once());
        _userRepositoryMock.Verify(m => m.Delete(user.Id), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_InvalidId_ThrowNotFoundException(Guid id)
    {
        //Arrange
        _userRepositoryMock.Setup(m => m.Delete(id));

        _userRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((UserEntity)null!);

        //Act
        //Assert
        await _userService.Invoking(x => x.Delete(id))
                            .Should().ThrowAsync<NotFoundException>();

        _userRepositoryMock.Verify(m => m.Get(id), Times.Once());
        _userRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Never());
    }
}
