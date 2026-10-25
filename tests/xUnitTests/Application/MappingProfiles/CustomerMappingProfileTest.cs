using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Requests.Customer;
using BillerContracts.Responses.Customer;
using Domain.Entities;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class CustomerMappingProfileTest
{
    [Fact]
    public void ToEntity_GivenModel_MapsAllProperties()
    {
        //Arrange
        var from = new CustomerModel
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            SeriesId = Guid.NewGuid(),
            SeriesName = "INV"
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
        var from = new CustomerEntity
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            SeriesId = Guid.NewGuid(),
            SeriesName = "INV"
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
        var from = new CustomerAddRequest
        {
            SellerId = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            SeriesId = Guid.NewGuid()
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
        var from = new CustomerUpdateRequest
        {
            Id = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            SeriesId = Guid.NewGuid()
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
        var from = new CustomerModel
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            SeriesId = Guid.NewGuid(),
            SeriesName = "INV"
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
        var from = new CustomerResponse
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            CompanyName = "CompanyName",
            CompanyNumber = "123456",
            Street = "Main St",
            City = "Vilnius",
            State = "LT",
            Email = "test@email.com",
            Phone = "+37060000000",
            SeriesId = Guid.NewGuid(),
            SeriesName = "INV"
        };

        //Act
        var to = from.ToModel();

        //Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }
}
