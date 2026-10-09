using Domain.Exceptions;
using FluentAssertions;

namespace xUnitTests.DomainTests;

public class ExceptionsTest
{
    public static IEnumerable<object[]> Factories()
    {
        yield return [new Func<Exception>(() => new NotFoundException()), new Func<string, Exception>(m => new NotFoundException(m)), new Func<string, Exception, Exception>((m, i) => new NotFoundException(m, i))];
        yield return [new Func<Exception>(() => new ClientAPIException()), new Func<string, Exception>(m => new ClientAPIException(m)), new Func<string, Exception, Exception>((m, i) => new ClientAPIException(m, i))];
    }

    [Theory]
    [MemberData(nameof(Factories))]
    public void Constructors_GivenArguments_SetMessageAndInnerException(
        Func<Exception> empty, Func<string, Exception> withMessage, Func<string, Exception, Exception> withInner)
    {
        //Arrange
        var inner = new InvalidOperationException("inner");

        //Act
        Exception defaultException = empty();
        Exception messageException = withMessage("message");
        Exception innerException = withInner("message", inner);

        //Assert
        defaultException.InnerException.Should().BeNull();
        messageException.Message.Should().Be("message");
        messageException.InnerException.Should().BeNull();
        innerException.Message.Should().Be("message");
        innerException.InnerException.Should().BeSameAs(inner);
    }
}
