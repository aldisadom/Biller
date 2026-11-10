using BillerContracts.Responses;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.Middleware;

namespace xUnitTests.WebAPI.Middleware;

public class ModelStateErrorsTest
{
    private readonly Mock<ILogger> _loggerMock = new(MockBehavior.Strict);
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new(MockBehavior.Strict);

    public ModelStateErrorsTest()
    {
        _loggerFactoryMock.Setup(f => f.CreateLogger(typeof(ModelStateErrors).FullName!))
            .Returns(_loggerMock.Object);
        _loggerMock.Setup(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    [Fact]
    public void ToErrorResponse_GivenOneError_ReturnsBadRequestWithItsMessage()
    {
        //Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("PageSize", "The value '7' is invalid.");

        //Act
        var result = ModelStateErrors.ToErrorResponse(CreateContext(modelState));

        //Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new ErrorResponse
            {
                Message = "Validation failure",
                ExtendedMessage = "The value '7' is invalid."
            });

        _loggerMock.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("The value '7' is invalid.")),
            null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once());
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void ToErrorResponse_GivenSeveralErrors_ReturnsOnePerLine()
    {
        //Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Page", "The value 'abc' is not valid for Page.");
        modelState.AddModelError("PageSize", "The value '7' is invalid.");

        //Act
        var result = ModelStateErrors.ToErrorResponse(CreateContext(modelState));

        //Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new ErrorResponse
            {
                Message = "Validation failure",
                ExtendedMessage = $"The value 'abc' is not valid for Page.{Environment.NewLine}The value '7' is invalid."
            });
    }

    [Fact]
    public void ToErrorResponse_GivenErrorWithOnlyException_HidesExceptionText()
    {
        //Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Body", new InvalidOperationException("Converter failed at Biller.Internal"), new EmptyModelMetadataProvider().GetMetadataForType(typeof(string)));

        //Act
        var result = ModelStateErrors.ToErrorResponse(CreateContext(modelState));

        //Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new ErrorResponse
            {
                Message = "Validation failure",
                ExtendedMessage = "The input was not valid."
            });
    }

    private ActionContext CreateContext(ModelStateDictionary modelState)
    {
        var services = new ServiceCollection()
            .AddSingleton(_loggerFactoryMock.Object)
            .BuildServiceProvider();

        return new(new DefaultHttpContext { RequestServices = services }, new RouteData(), new ActionDescriptor(), modelState);
    }
}
