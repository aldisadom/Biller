using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Enums;
using BillerContracts.Requests.Series;
using BillerContracts.Responses;
using BillerContracts.Responses.Series;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.Controllers;

namespace xUnitTests.WebAPI.Controllers;

public class SeriesControllerTest
{
    private readonly Mock<ISeriesService> _seriesServiceMock;
    private readonly Mock<IValidator<SeriesAddRequest>> _validatorAddMock;
    private readonly Mock<IValidator<SeriesUpdateRequest>> _validatorUpdateMock;
    private readonly SeriesController _seriesController;
    private readonly Guid _series1Id = Guid.NewGuid();
    private readonly Guid _series2Id = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();

    public SeriesControllerTest()
    {
        _seriesServiceMock = new Mock<ISeriesService>(MockBehavior.Strict);
        _validatorAddMock = new Mock<IValidator<SeriesAddRequest>>(MockBehavior.Strict);
        _validatorUpdateMock = new Mock<IValidator<SeriesUpdateRequest>>(MockBehavior.Strict);

        _seriesController = new SeriesController(
            _seriesServiceMock.Object,
            new Mock<ILogger<SeriesController>>().Object,
            _validatorAddMock.Object,
            _validatorUpdateMock.Object);
    }

    [Fact]
    public async Task Get_GivenValidId_ReturnsOkWithSeriesResponse()
    {
        //Arrange
        var series = new SeriesModel
        {
            Id = _series1Id,
            SellerId = _sellerId
        };

        _seriesServiceMock.Setup(s => s.Get(series.Id))
            .ReturnsAsync(series);

        //Act
        var result = await _seriesController.Get(series.Id);

        //Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(series.ToResponse());

        _seriesServiceMock.Verify(s => s.Get(series.Id), Times.Once());
        _seriesServiceMock.VerifyNoOtherCalls();
    }

    public static IEnumerable<TheoryDataRow<SeriesGetRequest?, int, PageSize>> GetQueryTestData()
    {
        yield return new(null, 1, PageSize.p25) { Label = "no query returns first page of 25" };
        yield return new(new SeriesGetRequest { SellerId = Guid.NewGuid(), Page = 3, PageSize = PageSize.p50 }, 3, PageSize.p50) { Label = "query page and page size are echoed" };
    }

    [Theory]
    [MemberData(nameof(GetQueryTestData))]
    public async Task Get_GivenQuery_ReturnsOkWithSeriesListResponse(SeriesGetRequest? query, int expectedPage, PageSize expectedPageSize)
    {
        //Arrange
        var series = new List<SeriesModel>
        {
            new() { Id = _series1Id, SellerId = _sellerId },
            new() { Id = _series2Id, SellerId = _sellerId }
        };

        _seriesServiceMock.Setup(s => s.Get(query))
            .ReturnsAsync((series, 7));

        //Act
        var result = await _seriesController.Get(query);

        //Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new SeriesListResponse
            {
                Series = series.Select(c => c.ToResponse()).ToList(),
                TotalCount = 7,
                Page = expectedPage,
                PageSize = expectedPageSize
            });

        _seriesServiceMock.Verify(s => s.Get(query), Times.Once());
        _seriesServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Add_GivenValidRequest_ReturnsCreatedWithAddResponse()
    {
        //Arrange
        var request = new SeriesAddRequest
        {
            SellerId = _sellerId,
            Name = "WF"
        };

        _validatorAddMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _seriesServiceMock.Setup(s => s.Add(request.ToModel()))
            .ReturnsAsync(_series1Id);

        //Act
        var result = await _seriesController.Add(request);

        //Assert
        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.Value.Should().BeEquivalentTo(new AddResponse { Id = _series1Id });

        _validatorAddMock.Verify(v => v.Validate(request), Times.Once());
        _validatorAddMock.VerifyNoOtherCalls();
        _seriesServiceMock.Verify(s => s.Add(request.ToModel()), Times.Once());
        _seriesServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_GivenValidRequest_ReturnsNoContent()
    {
        //Arrange
        var request = new SeriesUpdateRequest
        {
            Id = Guid.NewGuid()
        };

        _validatorUpdateMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _seriesServiceMock.Setup(s => s.Update(request.ToModel()))
            .Returns(Task.CompletedTask);

        //Act
        var result = await _seriesController.Update(request);

        //Assert
        result.Should().BeOfType<NoContentResult>();

        _validatorUpdateMock.Verify(v => v.Validate(request), Times.Once());
        _validatorUpdateMock.VerifyNoOtherCalls();
        _seriesServiceMock.Verify(s => s.Update(request.ToModel()), Times.Once());
        _seriesServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_GivenValidId_ReturnsNoContent()
    {
        //Arrange
        var id = Guid.NewGuid();

        _seriesServiceMock.Setup(s => s.Delete(id))
            .Returns(Task.CompletedTask);

        //Act
        var result = await _seriesController.Delete(id);

        //Assert
        result.Should().BeOfType<NoContentResult>();

        _seriesServiceMock.Verify(s => s.Delete(id), Times.Once());
        _seriesServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Add_GivenInvalidRequest_ThrowsValidationException()
    {
        //Arrange
        var request = new SeriesAddRequest();

        _validatorAddMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult([new ValidationFailure(nameof(request.Name), "invalid value")]));

        //Act
        Func<Task> act = () => _seriesController.Add(request);

        //Assert
        await act.Should().ThrowAsync<ValidationException>();

        _validatorAddMock.Verify(v => v.Validate(request), Times.Once());
        _validatorAddMock.VerifyNoOtherCalls();
        _seriesServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_GivenInvalidRequest_ThrowsValidationException()
    {
        //Arrange
        var request = new SeriesUpdateRequest();

        _validatorUpdateMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult([new ValidationFailure(nameof(request.Id), "invalid value")]));

        //Act
        Func<Task> act = () => _seriesController.Update(request);

        //Assert
        await act.Should().ThrowAsync<ValidationException>();

        _validatorUpdateMock.Verify(v => v.Validate(request), Times.Once());
        _validatorUpdateMock.VerifyNoOtherCalls();
        _seriesServiceMock.VerifyNoOtherCalls();
    }
}
