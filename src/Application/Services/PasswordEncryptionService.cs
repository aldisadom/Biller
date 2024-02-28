using Application.Interfaces;
using AutoMapper;
using Domain.IOptions;
using Domain.Repositories;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
