using Application.Models;
using Application.Services;
using AutoFixture.Xunit2;
using AutoMapper;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using WebAPI.MappingProfiles;

namespace xUnitTests.Application.Helpers.PriceToWords;

public class PriceToWordsLT
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly CustomerService _customerService;
    private readonly IMapper _mapper;

    public PriceToWordsLT()
    {
        _customerRepositoryMock = new Mock<ICustomerRepository>(MockBehavior.Strict);

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
        //sutvarkyt testa i ta ka reik
        //Arrange
        _customerRepositoryMock.Setup(m => m.Get(customer.Id))
                        .ReturnsAsync(customer);

        CustomerModel expectedResult = _mapper.Map<CustomerModel>(customer);

        //Act
        CustomerModel result = await _customerService.Get(customer.Id);

        //Assert
        result.Should().BeEquivalentTo(expectedResult);

        _customerRepositoryMock.Verify(m => m.Get(customer.Id), Times.Never());
    }
}
