using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Requests.User;
using Domain.Entities;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class UserMappingProfileTest
{
    [Fact]
    public void ToEntity_GivenModel_MapsAllProperties()
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
    public void ToModel_GivenEntity_MapsAllProperties()
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
    public void ToModel_GivenAddRequest_MapsAllProperties()
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
    public void ToModel_GivenUpdateRequest_MapsAllProperties()
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
    public void ToResponse_GivenModel_MapsAllProperties()
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
    public void ToModel_GivenLoginRequest_MapsAllProperties()
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
