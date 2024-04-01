using Application.Models;
using Application.Services;
using AutoFixture.Xunit2;
using AutoMapper;
using Contracts.Requests.Customer;
using Domain.Entities;

using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using WebAPI.MappingProfiles;

namespace xUnitTests.Services;

public class CustomerServiceTest
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly CustomerService _customerService;
    private readonly IMapper _mapper;

    public CustomerServiceTest()
    {
        _customerRepositoryMock = new Mock<ICustomerRepository>();

        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new CustomerMappingProfile());
        });
        mapperConfig.AssertConfigurationIsValid();
        _mapper = mapperConfig.CreateMapper();

        _customerService = new CustomerService(_customerRepositoryMock.Object, _mapper);
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenValidId_ReturnsDTO(CustomerEntity customer)
    {
        //Arrange
        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        CustomerModel expectedResult = _mapper.Map<CustomerModel>(customer);

        //Act
        CustomerModel result = await _customerService.Get(customer.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetId_GivenInvalidId_ThrowNotFoundException(Guid id)
    {
        // Arrange
        _customerRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((CustomerEntity)null!);

        // Act Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => await _customerService.Get(id));

        _customerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenEmptyQuery_ReturnsDTO(List<CustomerEntity> customerList)
    {
        //Arrange
        CustomerGetRequest request = new();

        _customerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(customerList);

        _customerRepositoryMock.Setup(m => m.GetBySeller(It.IsAny<Guid>()))
                        .ReturnsAsync((List<CustomerEntity>)null!);

        List<CustomerModel> expectedResult = _mapper.Map<List<CustomerModel>>(customerList);

        //Act
        var result = await _customerService.Get(request);

        //Assert
        result.Count().Should().Be(customerList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(), Times.Once());
        _customerRepositoryMock.Verify(m => m.GetBySeller(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenAddressIdQuery_ReturnsDTO(CustomerGetRequest request, List<CustomerEntity> customerList)
    {
        //Arrange

        _customerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync((List<CustomerEntity>)null!);

        _customerRepositoryMock.Setup(m => m.GetBySeller((Guid)request.SellerId!))
                        .ReturnsAsync(customerList);

        List<CustomerModel> expectedResult = _mapper.Map<List<CustomerModel>>(customerList);

        //Act
        var result = await _customerService.Get(request);

        //Assert
        result.Count().Should().Be(customerList.Count);

        _customerRepositoryMock.Verify(m => m.Get(), Times.Never());
        _customerRepositoryMock.Verify(m => m.GetBySeller(It.IsAny<Guid>()), Times.Once());
    }

    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        CustomerGetRequest request = new();
        List<CustomerEntity> customerList = [];

        //Arrange
        _customerRepositoryMock.Setup(m => m.GetBySeller(It.IsAny<Guid>()))
                        .ReturnsAsync(customerList);

        _customerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(customerList);

        // Act Assert
        var result = await _customerService.Get(request);

        result.Count().Should().Be(0);
        result.Should().BeEquivalentTo(new List<CustomerModel>());

        _customerRepositoryMock.Verify(m => m.Get(), Times.Once());
        _customerRepositoryMock.Verify(m => m.GetBySeller(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Add_GivenValidId_ReturnsGuid(CustomerModel customer)
    {
        //Arrange
        CustomerEntity customerEntity = _mapper.Map<CustomerEntity>(customer);

        _customerRepositoryMock.Setup(m => m.Add(It.Is<CustomerEntity>
                                (x => x == customerEntity)))
                                 .ReturnsAsync(customer.Id);

        //Act
        Guid result = await _customerService.Add(customer);

        //Assert
        result.Should().Be(customer.Id);

        _customerRepositoryMock.Verify(m => m.Add(It.IsAny<CustomerEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_ReturnsSuccess(CustomerModel customer)
    {
        //Arrange
        CustomerEntity customerEntity = _mapper.Map<CustomerEntity>(customer);

        _customerRepositoryMock.Setup(m => m.Update(It.Is<CustomerEntity>
                                (x => x == customerEntity)));

        _customerRepositoryMock.Setup(m => m.Get(customerEntity.Id))
                                .ReturnsAsync(customerEntity);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(customer))
                                        .Should().NotThrowAsync<Exception>();

        _customerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _customerRepositoryMock.Verify(m => m.Update(It.IsAny<CustomerEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_InvalidId_NotFoundException(CustomerModel customer)
    {
        //Arrange
        CustomerEntity customerEntity = _mapper.Map<CustomerEntity>(customer);

        _customerRepositoryMock.Setup(m => m.Update(It.Is<CustomerEntity>
                                (x => x == customerEntity)));

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync((CustomerEntity)null!);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(customer))
                            .Should().ThrowAsync<NotFoundException>();

        _customerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task UpdateInvoiceNumber_ReturnsSuccess(CustomerModel customer)
    {
        //Arrange
        CustomerEntity customerEntity = _mapper.Map<CustomerEntity>(customer);

        _customerRepositoryMock.Setup(m => m.UpdateInvoiceNumber(It.Is<CustomerEntity>
                                (x => x == customerEntity)));

        _customerRepositoryMock.Setup(m => m.Get(customerEntity.Id))
                                .ReturnsAsync(customerEntity);

        //Act
        //Assert
        await _customerService.Invoking(x => x.UpdateInvoiceNumber(customer.Id))
                                        .Should().NotThrowAsync<Exception>();

        _customerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _customerRepositoryMock.Verify(m => m.UpdateInvoiceNumber(It.IsAny<CustomerEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task UpdateInvoiceNumber_InvalidId_NotFoundException(CustomerModel customer)
    {
        //Arrange
        CustomerEntity customerEntity = _mapper.Map<CustomerEntity>(customer);

        _customerRepositoryMock.Setup(m => m.UpdateInvoiceNumber(It.Is<CustomerEntity>
                                (x => x == customerEntity)));

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync((CustomerEntity)null!);

        //Act
        //Assert
        await _customerService.Invoking(x => x.UpdateInvoiceNumber(customer.Id))
                            .Should().ThrowAsync<NotFoundException>();

        _customerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_ValidId(CustomerEntity customer)
    {
        //Arrange
        _customerRepositoryMock.Setup(m => m.Delete(customer.Id));

        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Delete(customer.Id))
                            .Should().NotThrowAsync<Exception>();

        _customerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _customerRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_InvalidId_ThrowNotFoundException(Guid id)
    {
        //Arrange
        _customerRepositoryMock.Setup(m => m.Delete(id));

        _customerRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((CustomerEntity)null!);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Delete(id))
                            .Should().ThrowAsync<NotFoundException>();

        _customerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }
}
