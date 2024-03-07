using Application.Interfaces;
using Domain.IOptions;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class PasswordEncryptionService : IPasswordEncryptionService
{
    private readonly string _passwordSalt;

    public PasswordEncryptionService(IOptions<PasswordEncryption> passwordEncryption)
    {
        _passwordSalt = passwordEncryption.Value.Salt
            ?? throw new ArgumentNullException($"Password salt is missing");
    }

    public string Encrypt(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, _passwordSalt);
    }
}
