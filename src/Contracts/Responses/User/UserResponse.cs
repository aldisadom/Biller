using Domain.Enums;

namespace Contracts.Responses.User;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
}

    add user types table
    fix examples
    creeate unittests