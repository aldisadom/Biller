using Application.Interfaces;
using Domain.IOptions;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class PasswordEncryptionService : IPasswordEncryptionService
{
    private readonly string _passwordPepper;

    public PasswordEncryptionService(IOptions<PasswordEncryption> passwordEncryption)
    {
        _passwordPepper = passwordEncryption.Value.Pepper;
        if (string.IsNullOrEmpty(_passwordPepper))
            throw new ArgumentNullException(_passwordPepper, "Password salt is missing");
    }

    public (string hashed, string saltUsed) Encrypt(string password, string? salt = null)
    {
        string encryptionSaltsalt = salt ?? BCrypt.Net.BCrypt.GenerateSalt(13);
        string hashed = BCrypt.Net.BCrypt.HashPassword(password + _passwordPepper, encryptionSaltsalt);
        return (hashed, encryptionSaltsalt);
    }
}
