using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Requests.Seller;
using BillerContracts.Responses.Seller;
using Domain.Entities;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class SellerMappingProfileTest
{
    [Fact]
    public void ToEntity_GivenModel_MapsAllProperties()
    {
        // Arrange
        var from = new SellerModel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            BankName = "Bank",
            BankNumber = "IBan"
        };

        // Act
        var to = from.ToEntity();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToModel_GivenEntity_MapsAllProperties()
    {
        // Arrange
        var from = new SellerEntity
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            BankName = "Bank",
            BankNumber = "IBan"
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
        var from = new SellerAddRequest
        {
            UserId = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            BankName = "Bank",
            BankNumber = "IBan"
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
        var from = new SellerUpdateRequest
        {
            Id = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            BankName = "Bank",
            BankNumber = "IBan"
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
        var from = new SellerModel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            BankName = "Bank",
            BankNumber = "IBan"
        };

        // Act
        var to = from.ToResponse();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToModel_GivenResponse_MapsAllProperties()
    {
        // Arrange
        var from = new SellerResponse
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            BankName = "Bank",
            BankNumber = "IBan"
        };

        // Act
        var to = from.ToModel();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }
}
