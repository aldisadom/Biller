using Contracts.Requests.User;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.User;

/// <summary>
/// example
/// </summary>
public class UserLoginRequestExample : IExamplesProvider<UserLoginRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public UserLoginRequest GetExamples()
    {
        return new UserLoginRequest()
        {
            Email = "WannabeStark@gmail.com",
            Password = "WinterIsComing"
        };
    }
}
