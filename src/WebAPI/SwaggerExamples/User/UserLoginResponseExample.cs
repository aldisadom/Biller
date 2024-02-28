using Contracts.Responses.User;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.User;

/// <summary>
/// example
/// </summary>
public class UserLoginResponseExample : IExamplesProvider<UserLoginResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public UserLoginResponse GetExamples()
    {
        return new UserLoginResponse()
        {
            Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"
        };
    }
}
