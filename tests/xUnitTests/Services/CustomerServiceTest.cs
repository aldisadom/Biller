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
    public async Task GetId_GivenValidId_ReturnsDTO(CustomerEntity Customer)
    {
        //Arrange
        _customerRepositoryMock.Setup(m => m.Get(Customer.Id))
                        .ReturnsAsync(Customer);

        CustomerModel expectedResult = _mapper.Map<CustomerModel>(Customer);

        //Act
        CustomerModel result = await _customerService.Get(Customer.Id);

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
    public async Task Get_GivenEmptyQuery_ReturnsDTO(List<CustomerEntity> CustomerList)
    {
        //Arrange
        CustomerGetRequest request = new();

        _customerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(CustomerList);

        _customerRepositoryMock.Setup(m => m.GetBySeller(It.IsAny<Guid>()))
                        .ReturnsAsync((List<CustomerEntity>)null!);

        List<CustomerModel> expectedResult = _mapper.Map<List<CustomerModel>>(CustomerList);

        //Act
        var result = await _customerService.Get(request);

        //Assert
        result.Count().Should().Be(CustomerList.Count);
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(), Times.Once());
        _customerRepositoryMock.Verify(m => m.GetBySeller(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Get_GivenAddressIdQuery_ReturnsDTO(CustomerGetRequest request, List<CustomerEntity> CustomerList)
    {
        //Arrange

        _customerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync((List<CustomerEntity>)null!);

        _customerRepositoryMock.Setup(m => m.GetBySeller((Guid)request.SellerId!))
                        .ReturnsAsync(CustomerList);

        List<CustomerModel> expectedResult = _mapper.Map<List<CustomerModel>>(CustomerList);

        //Act
        var result = await _customerService.Get(request);

        //Assert
        result.Count().Should().Be(CustomerList.Count);

        _customerRepositoryMock.Verify(m => m.Get(), Times.Never());
        _customerRepositoryMock.Verify(m => m.GetBySeller(It.IsAny<Guid>()), Times.Once());
    }

    [Fact]
    public async Task Get_GivenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        CustomerGetRequest request = new();
        List<CustomerEntity> CustomerList = [];

        //Arrange
        _customerRepositoryMock.Setup(m => m.GetBySeller(It.IsAny<Guid>()))
                        .ReturnsAsync(CustomerList);

        _customerRepositoryMock.Setup(m => m.Get())
                        .ReturnsAsync(CustomerList);

        // Act Assert
        var result = await _customerService.Get(request);

        result.Count().Should().Be(0);
        result.Should().BeEquivalentTo(new List<CustomerModel>());

        _customerRepositoryMock.Verify(m => m.Get(), Times.Once());
        _customerRepositoryMock.Verify(m => m.GetBySeller(It.IsAny<Guid>()), Times.Never());
    }

    [Theory]
    [AutoData]
    public async Task Add_GivenValidId_ReturnsGuid(CustomerModel Customer)
    {
        //Arrange
        CustomerEntity CustomerEntity = _mapper.Map<CustomerEntity>(Customer);

        _customerRepositoryMock.Setup(m => m.Add(It.Is<CustomerEntity>
                                (x => x == CustomerEntity)))
                                 .ReturnsAsync(Customer.Id);

        //Act
        Guid result = await _customerService.Add(Customer);

        //Assert
        result.Should().Be(Customer.Id);

        _customerRepositoryMock.Verify(m => m.Add(It.IsAny<CustomerEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_ReturnsSuccess(CustomerModel Customer)
    {
        //Arrange
        CustomerEntity CustomerEntity = _mapper.Map<CustomerEntity>(Customer);

        _customerRepositoryMock.Setup(m => m.Update(It.Is<CustomerEntity>
                                (x => x == CustomerEntity)));

        _customerRepositoryMock.Setup(m => m.Get(CustomerEntity.Id))
                                .ReturnsAsync(CustomerEntity);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(Customer))
                                        .Should().NotThrowAsync<Exception>();

        _customerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
        _customerRepositoryMock.Verify(m => m.Update(It.IsAny<CustomerEntity>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Update_InvalidId_NotFoundException(CustomerModel Customer)
    {
        //Arrange
        CustomerEntity CustomerEntity = _mapper.Map<CustomerEntity>(Customer);

        _customerRepositoryMock.Setup(m => m.Update(It.Is<CustomerEntity>
                                (x => x == CustomerEntity)));

        _customerRepositoryMock.Setup(m => m.Get(Customer.Id))
                        .ReturnsAsync((CustomerEntity)null!);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Update(Customer))
                            .Should().ThrowAsync<NotFoundException>();

        _customerRepositoryMock.Verify(m => m.Get(It.IsAny<Guid>()), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task Delete_ValidId(CustomerEntity Customer)
    {
        //Arrange
        _customerRepositoryMock.Setup(m => m.Delete(Customer.Id));

        _customerRepositoryMock.Setup(m => m.Get(Customer.Id))
                        .ReturnsAsync(Customer);

        //Act
        //Assert
        await _customerService.Invoking(x => x.Delete(Customer.Id))
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
