using System.ComponentModel.DataAnnotations;

namespace Contracts.Requests.User;

public class UserUpdateRequest
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string LastName { get; set; } = string.Empty;
}
