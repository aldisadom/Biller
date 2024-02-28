using Contracts.Requests.User;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.User;

/// <summary>
/// example
/// </summary>
public class UserAddRequestExample : IExamplesProvider<UserAddRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public UserAddRequest GetExamples()
    {
        return new UserAddRequest()
        {
            Name = "Jon",
            LastName = "Snow",
            Email = "WannabeStark@gmail.com",
            Password = "WinterIsComing"
        };
    }
}
