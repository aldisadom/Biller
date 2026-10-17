using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Requests.Item;
using BillerContracts.Responses.Item;
using Domain.Entities;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class ItemMappingProfileTest
{
    [Fact]
    public void ToEntity_GivenModel_MapsAllProperties()
    {
        //Arrange
        var from = new ItemModel
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M
        };

        //Act
        var to = from.ToEntity();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToModel_GivenEntity_MapsAllProperties()
    {
        //Arrange
        var from = new ItemEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M
        };


        //Act
        var to = from.ToModel();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToModel_GivenAddRequest_MapsAllProperties()
    {
        //Arrange
        var from = new ItemAddRequest
        {
            CustomerId = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M
        };

        //Act
        var to = from.ToModel();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom);
    }

    [Fact]
    public void ToModel_GivenUpdateRequest_MapsAllProperties()
    {
        //Arrange
        var from = new ItemUpdateRequest
        {
            Id = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M
        };

        //Act
        var to = from.ToModel();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom);
    }

    [Fact]
    public void ToResponse_GivenModel_MapsAllProperties()
    {
        //Arrange
        var from = new ItemModel
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M
        };

        //Act
        var to = from.ToResponse();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToModel_GivenResponse_MapsAllProperties()
    {
        //Arrange
        var from = new ItemResponse
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M
        };

        //Act
        var to = from.ToModel();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }
}
