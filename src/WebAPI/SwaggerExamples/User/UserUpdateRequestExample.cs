using Contracts.Requests.User;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.User;

/// <summary>
/// example
/// </summary>
public class UserUpdateRequestExample : IExamplesProvider<UserUpdateRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public UserUpdateRequest GetExamples()
    {
        return new UserUpdateRequest()
        {
            Id = Guid.NewGuid(),
            Name = "Jon",
            LastName = "Snow"
        };
    }
}
