using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Requests.Series;
using BillerContracts.Responses.Series;
using Domain.Entities;
using FluentAssertions;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class SeriesMappingProfileTest
{
    [Fact]
    public void ToEntity_GivenModel_MapsAllProperties()
    {
        //Arrange
        var from = new SeriesModel
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Name = "WF",
            NextNumber = 12
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
        var from = new SeriesEntity
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Name = "WF",
            NextNumber = 12
        };

        //Act
        var to = from.ToModel();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }

    [Fact]
    public void ToModel_GivenAddRequest_MapsAllPropertiesAndStartsAtOne()
    {
        //Arrange
        var from = new SeriesAddRequest
        {
            SellerId = Guid.NewGuid(),
            Name = "WF"
        };

        //Act
        var to = from.ToModel();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.UsedAllFrom);
        to.NextNumber.Should().Be(1);
    }

    [Theory]
    [InlineData(" WF", Label = "leading space is removed")]
    [InlineData("WF  ", Label = "trailing spaces are removed")]
    public void ToModel_GivenAddRequestNameWithSpaces_TrimsName(string name)
    {
        //Arrange
        var from = new SeriesAddRequest { Name = name };

        //Act
        var to = from.ToModel();

        //Assert
        to.Name.Should().Be("WF");
    }

    [Theory]
    [InlineData(" WF", Label = "leading space is removed")]
    [InlineData("WF  ", Label = "trailing spaces are removed")]
    public void ToModel_GivenUpdateRequestNameWithSpaces_TrimsName(string name)
    {
        //Arrange
        var from = new SeriesUpdateRequest { Name = name };

        //Act
        var to = from.ToModel();

        //Assert
        to.Name.Should().Be("WF");
    }

    [Fact]
    public void ToModel_GivenUpdateRequest_MapsAllProperties()
    {
        //Arrange
        var from = new SeriesUpdateRequest
        {
            Id = Guid.NewGuid(),
            Name = "WF",
            NextNumber = 12
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
        var from = new SeriesModel
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Name = "WF",
            NextNumber = 12
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
        var from = new SeriesResponse
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            Name = "WF",
            NextNumber = 12
        };

        //Act
        var to = from.ToModel();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }
}
