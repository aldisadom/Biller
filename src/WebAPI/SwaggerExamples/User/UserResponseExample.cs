using Contracts.Responses.User;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.User;

/// <summary>
/// example
/// </summary>
public class UserResponseExample : IExamplesProvider<UserResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public UserResponse GetExamples()
    {
        return new UserResponse()
        {
            Id = Guid.NewGuid(),
            Name = "Jon",
            LastName = "Snow",
            Email = "WannabeStark@gmail.com"
        };
    }
}
