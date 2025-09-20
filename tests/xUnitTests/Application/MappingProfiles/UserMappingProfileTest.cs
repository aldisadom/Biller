using Application.MappingProfiles;
using Application.Models;
using Contracts.Requests.User;
using Domain.Entities;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class UserMappingProfileTest
{
    [Fact]
    public void ToEntity_FromModel_ShouldMap()
    {
        // Arrange
        var from = new UserModel
        {
            Id = Guid.NewGuid(),
            Name = "UserName",
            LastName = "UserLastName",
            Email = "user@email.com",
            Password = "hashedPassword"
        };

        // Act
        var to = from.ToEntity();

        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom);
    }

    [Fact]
    public void ToModel_FromEntity_ShouldMap()
    {
        // Arrange
        var from = new UserEntity
        {
            Id = Guid.NewGuid(),
            Name = "UserName",
            LastName = "UserLastName",
            Email = "user@email.com",
            Password = "hashedPassword",
            Salt = "saltValue"
        };

        // Act
        var to = from.ToModel();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToModel_FromAddRequest_ShouldMap()
    {
        // Arrange
        var from = new UserAddRequest
        {
            Name = "UserName",
            LastName = "UserLastName",
            Email = "user@email.com",
            Password = "hashedPassword"
        };

        // Act
        var to = from.ToModel();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom);
    }

    [Fact]
    public void ToModel_FromUpdateRequest_ShouldMap()
    {
        // Arrange
        var from = new UserUpdateRequest
        {
            Id = Guid.NewGuid(),
            Name = "UserName",
            LastName = "UserLastName"
        };

        // Act
        var to = from.ToModel();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom);
    }

    [Fact]
    public void ToResponse_FromModel_ShouldMap()
    {
        // Arrange
        var from = new UserModel
        {
            Id = Guid.NewGuid(),
            Name = "UserName",
            LastName = "UserLastName",
            Email = "user@email.com",
            Password = "hashedPassword"
        };

        // Act
        var to = from.ToResponse();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToModel_FromLoginRequest_ShouldMap()
    {
        // Arrange
        var from = new UserLoginRequest
        {
            Email = "user@email.com",
            Password = "hashedPassword"
        };

        // Act
        var to = from.ToModel();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom);
    }
}
