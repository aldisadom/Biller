using Application.MappingProfiles;
using Application.Models;
using AwesomeAssertions;
using BillerContracts.Requests.Item;
using Domain.Entities;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class ItemMappingProfileTest
{
    [Fact]
    public void ToEntity_GivenModel_MapsAllPropertiesButVersion()
    {
        //Arrange
        var from = new ItemModel
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Name = "ItemName",
            Quantity = 66,
            Price = 123.67M,
            Version = 3
        };

        //Act
        var to = from.ToEntity();

        //Assert
        HashSet<string> ignoreProperties = ["Version"];
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo, ignoreProperties);

        // A write never carries a version: the repository numbers them
        to.Version.Should().Be(0);
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
            Price = 123.67M,
            Version = 3
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
            Price = 123.67M,
            Version = 3
        };

        //Act
        var to = from.ToResponse();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToModel_GivenVersionEntity_MapsAllProperties()
    {
        //Arrange
        var from = new ItemVersionEntity
        {
            ItemId = Guid.NewGuid(),
            Version = 3,
            Name = "ItemName",
            Price = 123.67M,
            ChangedDate = new DateTime(2026, 11, 10, 14, 30, 0, DateTimeKind.Utc)
        };

        //Act
        var to = from.ToModel();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToResponse_GivenVersionModel_MapsAllProperties()
    {
        //Arrange
        var from = new ItemVersionModel
        {
            ItemId = Guid.NewGuid(),
            Version = 3,
            Name = "ItemName",
            Price = 123.67M,
            ChangedDate = new DateTime(2026, 11, 10, 14, 30, 0, DateTimeKind.Utc)
        };

        //Act
        var to = from.ToResponse();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }
}
