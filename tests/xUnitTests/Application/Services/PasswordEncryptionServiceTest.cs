using Application.Services;
using Domain.IOptions;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace xUnitTests.Application.Services;

public class PasswordEncryptionServiceTest
{
    private const string _pepper = "random pepper";

    [Fact]
    public void Encrypt_GivenWithoutSalt_ReturnsEncryptedAndRandomSalt()
    {
        //Arrange        
        Mock<IOptions<PasswordEncryption>> _passwordEncryption = new();
        _passwordEncryption.Setup(x => x.Value)
            .Returns(new PasswordEncryption()
            {
                Pepper = _pepper
            });

        PasswordEncryptionService _passwordEncryptionService = new(_passwordEncryption.Object);

        //Act
        (string hashed, string salt) = _passwordEncryptionService.Encrypt("MyPassword");

        //Assert
        hashed.Should().StartWith("$2a$13$");
        salt.Should().StartWith("$2a$13$");

        _passwordEncryption.Verify(m => m.Value, Times.Once());
    }

    [Fact]
    public void Encrypt_GivenWithSalt_ReturnsEncrypted()
    {
        //Arrange
        string salt = "$2a$10$TaqaG43aDC1Ai75moe.ENO";
        Mock<IOptions<PasswordEncryption>> _passwordEncryption = new();
        _passwordEncryption.Setup(x => x.Value)
            .Returns(new PasswordEncryption()
            {
                Pepper = _pepper
            });

        PasswordEncryptionService _passwordEncryptionService = new(_passwordEncryption.Object);

        //Act
        (string hashed, string encryptionSalt) = _passwordEncryptionService.Encrypt("MyPassword", salt);

        //Assert
        hashed.Should().BeEquivalentTo("$2a$10$TaqaG43aDC1Ai75moe.ENOnhvzNttzNShq1XnlVCEFnqcWgI7IN72");
        encryptionSalt.Should().BeEquivalentTo(salt);

        _passwordEncryption.Verify(m => m.Value, Times.Once());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Encrypt_GivenInValidOptions_ThrowsException(string? pepper)
    {
        //Arrange        
        Mock<IOptions<PasswordEncryption>> _passwordEncryption = new();
        _passwordEncryption.Setup(x => x.Value)
            .Returns(new PasswordEncryption()
            {
                Pepper = pepper!
            });
        bool pass = false;

        //Act
        try
        {
            PasswordEncryptionService _passwordEncryptionService = new(_passwordEncryption.Object);
        }
        catch (ArgumentNullException)
        {
            pass = true;
        }

        //Assert
        pass.Should().BeTrue();
        _passwordEncryption.Verify(m => m.Value, Times.Once());
    }
}
