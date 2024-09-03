using Application.Services;
using Domain.IOptions;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace xUnitTests.Application.Services;

public class PasswordEncryptionServiceTest
{
    private const string _salt = "$2a$10$TaqaG43aDC1Ai75moe.ENO";

    [Fact]
    public void Encrypt_GivenValidOptions_ReturnsEncrypted()
    {
        //Arrange        
        Mock<IOptions<PasswordEncryption>>  _passwordEncryption = new Mock<IOptions<PasswordEncryption>>();
        _passwordEncryption.Setup(x => x.Value)
            .Returns(new PasswordEncryption()
            {
                Salt = _salt
            });

        PasswordEncryptionService _passwordEncryptionService = new PasswordEncryptionService(_passwordEncryption.Object);

        //Act
        string result = _passwordEncryptionService.Encrypt("MyPassword");

        //Assert
        result.Should().Be("$2a$10$TaqaG43aDC1Ai75moe.ENOeEn21HFpccM/Zt9GlkK7JxOAcWk5Dwi");

        _passwordEncryption.Verify(m => m.Value, Times.Once());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Encrypt_GivenInValidOptions_ThrowsException(string salt)
    {
        //Arrange        
        Mock<IOptions<PasswordEncryption>> _passwordEncryption = new Mock<IOptions<PasswordEncryption>>();
        _passwordEncryption.Setup(x => x.Value)
            .Returns(new PasswordEncryption()
            {
                Salt = salt
            });
        bool pass = false;

        //Act
        try
        {
            PasswordEncryptionService _passwordEncryptionService = new PasswordEncryptionService(_passwordEncryption.Object);
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
