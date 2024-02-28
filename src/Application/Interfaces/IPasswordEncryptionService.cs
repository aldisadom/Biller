namespace Application.Interfaces;

public interface IPasswordEncryptionService
{
    string Encrypt(string password);
}