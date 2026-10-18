using Application.Services;
using Domain.IOptions;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace xUnitTests.Application.Services;

public class PasswordEncryptionServiceTest
{
    private const string _pepper = "random pepper";

    private static Mock<IOptions<PasswordEncryption>> NewOptionsMock(string pepper)
    {
        Mock<IOptions<PasswordEncryption>> optionsMock = new(MockBehavior.Strict);
        optionsMock.Setup(x => x.Value)
            .Returns(new PasswordEncryption() { Pepper = pepper });

        return optionsMock;
    }

    [Fact]
    public void Encrypt_GivenWithoutSalt_ReturnsEncryptedAndRandomSalt()
    {
        //Arrange
        Mock<IOptions<PasswordEncryption>> optionsMock = NewOptionsMock(_pepper);
        PasswordEncryptionService passwordEncryptionService = new(optionsMock.Object);

        //Act
        (string hashed, string salt) = passwordEncryptionService.Encrypt("MyPassword");

        //Assert
        hashed.Should().StartWith("$2a$13$");
        salt.Should().StartWith("$2a$13$");

        optionsMock.Verify(m => m.Value, Times.Once());
    }

    [Fact]
    public void Encrypt_GivenWithSalt_ReturnsEncrypted()
    {
        //Arrange
        string salt = "$2a$10$TaqaG43aDC1Ai75moe.ENO";
        Mock<IOptions<PasswordEncryption>> optionsMock = NewOptionsMock(_pepper);
        PasswordEncryptionService passwordEncryptionService = new(optionsMock.Object);

        //Act
        (string hashed, string encryptionSalt) = passwordEncryptionService.Encrypt("MyPassword", salt);

        //Assert
        hashed.Should().Be("$2a$10$TaqaG43aDC1Ai75moe.ENOnhvzNttzNShq1XnlVCEFnqcWgI7IN72");
        encryptionSalt.Should().Be(salt);

        optionsMock.Verify(m => m.Value, Times.Once());
    }

    [Theory]
    [InlineData(null, Label = "missing pepper throws")]
    [InlineData("", Label = "empty pepper throws")]
    public void Encrypt_GivenInValidOptions_ThrowsException(string? pepper)
    {
        //Arrange
        Mock<IOptions<PasswordEncryption>> optionsMock = NewOptionsMock(pepper!);

        //Act
        Action act = () => new PasswordEncryptionService(optionsMock.Object);

        //Assert
        act.Should().Throw<ArgumentNullException>();

        optionsMock.Verify(m => m.Value, Times.Once());
    }
}
