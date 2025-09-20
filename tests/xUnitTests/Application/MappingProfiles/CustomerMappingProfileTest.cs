using Application.MappingProfiles;
using Application.Models;
using Contracts.Requests.Customer;
using Contracts.Responses.Customer;
using Domain.Entities;
using static xUnitTests.Application.MappingProfiles.MappingTestHelper;

namespace xUnitTests.Application.MappingProfiles;

public class CustomerMappingProfileTest
{
    [Fact]
    public void ToEntity_FromModel_ShouldMap()
    {
        // Arrange
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
            InvoiceName = "INV",
            InvoiceNumber = 6
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
            InvoiceName = "INV",
            InvoiceNumber = 6
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
            InvoiceName = "INV"
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
            InvoiceName = "INV",
            InvoiceNumber = 6
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
            InvoiceName = "INV",
            InvoiceNumber = 6
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
            InvoiceName = "INV",
            InvoiceNumber = 6
        };

        // Act
        var to = from.ToModel();

        // Assert
        MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo);
    }
}
