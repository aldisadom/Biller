using Application.Interfaces;
using Application.Models;
using Application.Services;
using BillerContracts;
using BillerContracts.Enums;
using BillerContracts.Requests.Series;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using Moq;
using System.Net;

namespace xUnitTests.Application.Services;

public class SeriesServiceTest
{
    private readonly Mock<ISeriesRepository> _seriesRepositoryMock;
    private readonly Mock<ISellerService> _sellerServiceMock;
    private readonly SeriesService _seriesService;

    public SeriesServiceTest()
    {
        _seriesRepositoryMock = new Mock<ISeriesRepository>(MockBehavior.Strict);
        _sellerServiceMock = new Mock<ISellerService>(MockBehavior.Strict);

        _seriesService = new SeriesService(_seriesRepositoryMock.Object, _sellerServiceMock.Object);
    }

    [Fact]
    public async Task GetId_GivenValidId_ReturnsModel()
    {
        //Arrange
        SeriesEntity series = new() { Id = Guid.NewGuid(), Name = "WF" };

        _seriesRepositoryMock.Setup(m => m.Get(series.Id))
                        .ReturnsAsync(series);

        //Act
        SeriesModel result = await _seriesService.Get(series.Id);

        //Assert
        result.Should().BeEquivalentTo(new SeriesModel { Id = series.Id, Name = "WF" });

        _seriesRepositoryMock.Verify(m => m.Get(series.Id), Times.Once());
    }

    [Fact]
    public async Task GetId_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _seriesRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((SeriesEntity)null!);

        //Act
        //Assert
        await _seriesService.Invoking(x => x.Get(id))
                            .Should().ThrowAsync<NotFoundException>();

        _seriesRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    public static IEnumerable<TheoryDataRow<SeriesGetRequest?, Guid?, int, int>> GetQueryTestData()
    {
        Guid sellerId = Guid.NewGuid();

        yield return new(null, null, 1, 25) { Label = "no query reads the first 25 series of all sellers" };
        yield return new(new SeriesGetRequest(), null, 1, 25) { Label = "empty query reads the first 25 series of all sellers" };
        yield return new(new SeriesGetRequest { SellerId = sellerId, Page = 2, PageSize = PageSize.p50 }, sellerId, 2, 50) { Label = "seller filter and paging are passed to the repository" };
    }

    [Theory]
    [MemberData(nameof(GetQueryTestData))]
    public async Task Get_GivenQuery_PassesFilterToRepository(SeriesGetRequest? query, Guid? sellerId, int page, int pageSize)
    {
        //Arrange
        List<SeriesEntity> series = [new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() }];

        _seriesRepositoryMock.Setup(m => m.Get(sellerId, page, pageSize))
                        .ReturnsAsync((series, 9));

        //Act
        var (result, totalCount) = await _seriesService.Get(query);

        //Assert
        result.Select(s => s.Id).Should().Equal(series.Select(s => s.Id));
        totalCount.Should().Be(9);

        _seriesRepositoryMock.Verify(m => m.Get(sellerId, page, pageSize), Times.Once());
    }

    public static IEnumerable<TheoryDataRow<SeriesEntity?, Guid, bool>> GetWithValidationTestData()
    {
        Guid sellerId = Guid.NewGuid();

        yield return new(new SeriesEntity { Id = Guid.NewGuid(), SellerId = sellerId }, sellerId, true) { Label = "series of the seller is valid" };
        yield return new(new SeriesEntity { Id = Guid.NewGuid(), SellerId = Guid.NewGuid() }, sellerId, false) { Label = "series of another seller is invalid" };
        yield return new(null, sellerId, false) { Label = "missing series is invalid" };
    }

    [Theory]
    [MemberData(nameof(GetWithValidationTestData))]
    public async Task GetWithValidation_GivenSeries_ReturnsExpected(SeriesEntity? series, Guid sellerId, bool expected)
    {
        //Arrange
        Guid id = series?.Id ?? Guid.NewGuid();

        _seriesRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync(series);

        //Act
        Result<SeriesModel> result = await _seriesService.GetWithValidation(id, sellerId);

        //Assert
        bool isValid = result.Match(_ => true, _ => false);
        isValid.Should().Be(expected);

        if (!expected)
            result.Match(_ => null!, error => error).Should().BeEquivalentTo(new ErrorModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Validation failure",
                ExtendedMessage = $"Series id {id} is invalid for seller id {sellerId}"
            });

        _seriesRepositoryMock.Verify(m => m.Get(id), Times.Once());
    }

    [Fact]
    public async Task Add_GivenFreeName_ReturnsGuid()
    {
        //Arrange
        SeriesModel series = new() { SellerId = Guid.NewGuid(), Name = "WF", NextNumber = 1 };
        SeriesEntity seriesEntity = new() { SellerId = series.SellerId, Name = "WF", NextNumber = 1 };
        Guid id = Guid.NewGuid();

        _sellerServiceMock.Setup(m => m.Get(series.SellerId))
                        .ReturnsAsync(new SellerModel { Id = series.SellerId });
        _seriesRepositoryMock.Setup(m => m.GetByName(series.SellerId, "WF"))
                        .ReturnsAsync((SeriesEntity)null!);
        _seriesRepositoryMock.Setup(m => m.Add(seriesEntity))
                        .ReturnsAsync(id);

        //Act
        Guid result = await _seriesService.Add(series);

        //Assert
        result.Should().Be(id);

        _sellerServiceMock.Verify(m => m.Get(series.SellerId), Times.Once());
        _seriesRepositoryMock.Verify(m => m.GetByName(series.SellerId, "WF"), Times.Once());
        _seriesRepositoryMock.Verify(m => m.Add(seriesEntity), Times.Once());
    }

    [Fact]
    public async Task Add_GivenMissingSeller_ThrowsNotFoundException()
    {
        //Arrange
        SeriesModel series = new() { SellerId = Guid.NewGuid(), Name = "WF" };

        _sellerServiceMock.Setup(m => m.Get(series.SellerId))
                        .ThrowsAsync(new NotFoundException("seller not found"));

        //Act
        //Assert
        await _seriesService.Invoking(x => x.Add(series))
                            .Should().ThrowAsync<NotFoundException>();

        _sellerServiceMock.Verify(m => m.Get(series.SellerId), Times.Once());
        _seriesRepositoryMock.Verify(m => m.Add(It.IsAny<SeriesEntity>()), Times.Never());
    }

    [Fact]
    public async Task Add_GivenNameTakenBySameSeller_ThrowsValidationException()
    {
        //Arrange
        SeriesModel series = new() { SellerId = Guid.NewGuid(), Name = "WF" };

        _sellerServiceMock.Setup(m => m.Get(series.SellerId))
                        .ReturnsAsync(new SellerModel { Id = series.SellerId });
        _seriesRepositoryMock.Setup(m => m.GetByName(series.SellerId, "WF"))
                        .ReturnsAsync(new SeriesEntity { Id = Guid.NewGuid(), SellerId = series.SellerId, Name = "WF" });

        //Act
        //Assert
        await _seriesService.Invoking(x => x.Add(series))
                            .Should().ThrowAsync<ValidationException>()
                            .WithMessage($"Series WF already exists for seller id {series.SellerId}");

        _sellerServiceMock.Verify(m => m.Get(series.SellerId), Times.Once());
        _seriesRepositoryMock.Verify(m => m.GetByName(series.SellerId, "WF"), Times.Once());
        _seriesRepositoryMock.Verify(m => m.Add(It.IsAny<SeriesEntity>()), Times.Never());
    }

    public static IEnumerable<TheoryDataRow<SeriesEntity?>> GetFreeNameOnUpdateTestData()
    {
        yield return new((SeriesEntity?)null) { Label = "no series has the name" };
        yield return new(new SeriesEntity { Name = "KL" }) { Label = "only the updated series itself has the name" };
    }

    [Theory]
    [MemberData(nameof(GetFreeNameOnUpdateTestData))]
    public async Task Update_GivenFreeNameAndNumberAboveInvoices_UpdatesWithStoredSeller(SeriesEntity? sameName)
    {
        //Arrange
        SeriesEntity stored = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Name = "WF", NextNumber = 4 };
        SeriesModel series = new() { Id = stored.Id, SellerId = Guid.NewGuid(), Name = "KL", NextNumber = 6 };
        SeriesEntity expected = new() { Id = stored.Id, SellerId = stored.SellerId, Name = "KL", NextNumber = 6 };
        if (sameName is not null)
            sameName.Id = stored.Id;

        _seriesRepositoryMock.Setup(m => m.Get(stored.Id))
                        .ReturnsAsync(stored);
        _seriesRepositoryMock.Setup(m => m.GetByName(stored.SellerId, "KL"))
                        .ReturnsAsync(sameName);
        _seriesRepositoryMock.Setup(m => m.Update(expected, stored.NextNumber))
                        .ReturnsAsync(true);

        //Act
        await _seriesService.Update(series);

        //Assert
        _seriesRepositoryMock.Verify(m => m.Get(stored.Id), Times.Once());
        _seriesRepositoryMock.Verify(m => m.GetByName(stored.SellerId, "KL"), Times.Once());
        _seriesRepositoryMock.Verify(m => m.Update(expected, stored.NextNumber), Times.Once());
        _seriesRepositoryMock.Verify(m => m.GetMaxInvoiceNumber(It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    public async Task Update_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        SeriesModel series = new() { Id = Guid.NewGuid() };

        _seriesRepositoryMock.Setup(m => m.Get(series.Id))
                        .ReturnsAsync((SeriesEntity)null!);

        //Act
        //Assert
        await _seriesService.Invoking(x => x.Update(series))
                            .Should().ThrowAsync<NotFoundException>();

        _seriesRepositoryMock.Verify(m => m.Get(series.Id), Times.Once());
        _seriesRepositoryMock.Verify(m => m.Update(It.IsAny<SeriesEntity>(), It.IsAny<int>()), Times.Never());
    }

    [Fact]
    public async Task Update_GivenNameTakenByOtherSeries_ThrowsValidationException()
    {
        //Arrange
        SeriesEntity stored = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Name = "WF" };
        SeriesModel series = new() { Id = stored.Id, Name = "KL", NextNumber = 6 };

        _seriesRepositoryMock.Setup(m => m.Get(stored.Id))
                        .ReturnsAsync(stored);
        _seriesRepositoryMock.Setup(m => m.GetByName(stored.SellerId, "KL"))
                        .ReturnsAsync(new SeriesEntity { Id = Guid.NewGuid(), SellerId = stored.SellerId, Name = "KL" });

        //Act
        //Assert
        await _seriesService.Invoking(x => x.Update(series))
                            .Should().ThrowAsync<ValidationException>()
                            .WithMessage($"Series KL already exists for seller id {stored.SellerId}");

        _seriesRepositoryMock.Verify(m => m.Get(stored.Id), Times.Once());
        _seriesRepositoryMock.Verify(m => m.GetByName(stored.SellerId, "KL"), Times.Once());
        _seriesRepositoryMock.Verify(m => m.Update(It.IsAny<SeriesEntity>(), It.IsAny<int>()), Times.Never());
    }

    [Theory]
    [InlineData(5, Label = "next number equal to the highest invoice number is rejected")]
    [InlineData(2, Label = "next number below the highest invoice number is rejected")]
    public async Task Update_GivenNextNumberNotAboveInvoices_ThrowsValidationException(int nextNumber)
    {
        //Arrange
        SeriesEntity stored = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Name = "WF" };
        SeriesModel series = new() { Id = stored.Id, Name = "WF", NextNumber = nextNumber };
        SeriesEntity expected = new() { Id = stored.Id, SellerId = stored.SellerId, Name = "WF", NextNumber = nextNumber };

        _seriesRepositoryMock.Setup(m => m.Get(stored.Id))
                        .ReturnsAsync(stored);
        _seriesRepositoryMock.Setup(m => m.GetByName(stored.SellerId, "WF"))
                        .ReturnsAsync(stored);
        _seriesRepositoryMock.Setup(m => m.Update(expected, stored.NextNumber))
                        .ReturnsAsync(false);
        _seriesRepositoryMock.Setup(m => m.GetMaxInvoiceNumber(stored.Id))
                        .ReturnsAsync(5);

        //Act
        //Assert
        await _seriesService.Invoking(x => x.Update(series))
                            .Should().ThrowAsync<ValidationException>()
                            .WithMessage("Next number must be greater than 5, the highest invoice number in series WF");

        _seriesRepositoryMock.Verify(m => m.Get(stored.Id), Times.Exactly(2));
        _seriesRepositoryMock.Verify(m => m.GetByName(stored.SellerId, "WF"), Times.Once());
        _seriesRepositoryMock.Verify(m => m.Update(expected, stored.NextNumber), Times.Once());
        _seriesRepositoryMock.Verify(m => m.GetMaxInvoiceNumber(stored.Id), Times.Once());
    }

    [Fact]
    public async Task Update_GivenInvoiceChangedSeriesMeanwhile_ThrowsValidationException()
    {
        //Arrange
        SeriesEntity stored = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Name = "WF" };
        SeriesModel series = new() { Id = stored.Id, Name = "WF", NextNumber = 4 };
        SeriesEntity expected = new() { Id = stored.Id, SellerId = stored.SellerId, Name = "WF", NextNumber = 4 };

        _seriesRepositoryMock.Setup(m => m.Get(stored.Id))
                        .ReturnsAsync(stored);
        _seriesRepositoryMock.Setup(m => m.GetByName(stored.SellerId, "WF"))
                        .ReturnsAsync(stored);
        _seriesRepositoryMock.Setup(m => m.Update(expected, stored.NextNumber))
                        .ReturnsAsync(false);
        _seriesRepositoryMock.Setup(m => m.GetMaxInvoiceNumber(stored.Id))
                        .ReturnsAsync(3);

        //Act
        //Assert
        await _seriesService.Invoking(x => x.Update(series))
                            .Should().ThrowAsync<ValidationException>()
                            .WithMessage("Series WF got a new or deleted invoice while it was updated, please try again");

        _seriesRepositoryMock.Verify(m => m.Get(stored.Id), Times.Exactly(2));
        _seriesRepositoryMock.Verify(m => m.GetByName(stored.SellerId, "WF"), Times.Once());
        _seriesRepositoryMock.Verify(m => m.Update(expected, stored.NextNumber), Times.Once());
        _seriesRepositoryMock.Verify(m => m.GetMaxInvoiceNumber(stored.Id), Times.Once());
    }

    [Fact]
    public async Task Update_GivenSeriesDeletedMeanwhile_ThrowsNotFoundException()
    {
        //Arrange
        SeriesEntity stored = new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid(), Name = "WF", NextNumber = 1 };
        SeriesModel series = new() { Id = stored.Id, Name = "WF", NextNumber = 4 };
        SeriesEntity expected = new() { Id = stored.Id, SellerId = stored.SellerId, Name = "WF", NextNumber = 4 };

        _seriesRepositoryMock.SetupSequence(m => m.Get(stored.Id))
                        .ReturnsAsync(stored)
                        .ReturnsAsync((SeriesEntity)null!);
        _seriesRepositoryMock.Setup(m => m.GetByName(stored.SellerId, "WF"))
                        .ReturnsAsync(stored);
        _seriesRepositoryMock.Setup(m => m.Update(expected, stored.NextNumber))
                        .ReturnsAsync(false);

        //Act
        //Assert
        await _seriesService.Invoking(x => x.Update(series))
                            .Should().ThrowAsync<NotFoundException>();

        _seriesRepositoryMock.Verify(m => m.Get(stored.Id), Times.Exactly(2));
        _seriesRepositoryMock.Verify(m => m.GetByName(stored.SellerId, "WF"), Times.Once());
        _seriesRepositoryMock.Verify(m => m.Update(expected, stored.NextNumber), Times.Once());
        _seriesRepositoryMock.Verify(m => m.GetMaxInvoiceNumber(It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    public async Task Delete_GivenUnusedSeries_DeletesFromRepository()
    {
        //Arrange
        SeriesEntity series = new() { Id = Guid.NewGuid() };

        _seriesRepositoryMock.Setup(m => m.Get(series.Id))
                        .ReturnsAsync(series);
        _seriesRepositoryMock.Setup(m => m.IsInUse(series.Id))
                        .ReturnsAsync(false);
        _seriesRepositoryMock.Setup(m => m.Delete(series.Id))
                        .Returns(Task.CompletedTask);

        //Act
        await _seriesService.Delete(series.Id);

        //Assert
        _seriesRepositoryMock.Verify(m => m.Get(series.Id), Times.Once());
        _seriesRepositoryMock.Verify(m => m.IsInUse(series.Id), Times.Once());
        _seriesRepositoryMock.Verify(m => m.Delete(series.Id), Times.Once());
    }

    [Fact]
    public async Task Delete_GivenSeriesInUse_ThrowsValidationException()
    {
        //Arrange
        SeriesEntity series = new() { Id = Guid.NewGuid() };

        _seriesRepositoryMock.Setup(m => m.Get(series.Id))
                        .ReturnsAsync(series);
        _seriesRepositoryMock.Setup(m => m.IsInUse(series.Id))
                        .ReturnsAsync(true);

        //Act
        //Assert
        await _seriesService.Invoking(x => x.Delete(series.Id))
                            .Should().ThrowAsync<ValidationException>()
                            .WithMessage($"Series id {series.Id} is used by customers or invoices");

        _seriesRepositoryMock.Verify(m => m.Get(series.Id), Times.Once());
        _seriesRepositoryMock.Verify(m => m.IsInUse(series.Id), Times.Once());
        _seriesRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    public async Task Delete_GivenInvalidId_ThrowsNotFoundException()
    {
        //Arrange
        Guid id = Guid.NewGuid();

        _seriesRepositoryMock.Setup(m => m.Get(id))
                        .ReturnsAsync((SeriesEntity)null!);

        //Act
        //Assert
        await _seriesService.Invoking(x => x.Delete(id))
                            .Should().ThrowAsync<NotFoundException>();

        _seriesRepositoryMock.Verify(m => m.Get(id), Times.Once());
        _seriesRepositoryMock.Verify(m => m.Delete(It.IsAny<Guid>()), Times.Never());
    }
}
