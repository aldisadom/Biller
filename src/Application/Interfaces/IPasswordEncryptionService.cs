namespace Application.Interfaces;

public interface IPasswordEncryptionService
{
    (string hashed, string saltUsed) Encrypt(string password, string? salt = null);
}
