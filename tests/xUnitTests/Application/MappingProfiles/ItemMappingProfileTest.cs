using Application.MappingProfiles;
using Application.Models;
using Contracts.Requests.Item;
using Contracts.Responses.Item;
using Domain.Entities;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class ItemMappingProfileTest
{
    [Fact]
    public void ToEntity_FromModel_ShouldMap()
    {
        // Arrange
        var from = new ItemModel
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M
        };

        // Act
        var to = from.ToEntity();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToModel_FromEntity_ShouldMap()
    {
        // Arrange
        var from = new ItemEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M
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
        var from = new ItemAddRequest
        {
            CustomerId = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M
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
        var from = new ItemUpdateRequest
        {
            Id = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M
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
        var from = new ItemModel
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M
        };

        // Act
        var to = from.ToResponse();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToModel_FromResponse_ShouldMap()
    {
        // Arrange
        var from = new ItemResponse
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M
        };

        // Act
        var to = from.ToModel();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }
}
