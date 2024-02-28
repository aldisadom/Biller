using Contracts.Responses.User;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.User;

/// <summary>
/// example
/// </summary>
public class UserListResponseExample : IExamplesProvider<UserListResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public UserListResponse GetExamples()
    {
        UserListResponse userList = new();

        userList.Users.Add(new UserResponse()
        {
            Id = Guid.NewGuid(),
            Name = "Jon",
            LastName = "Snow",
            Email = "WannabeStark@gmail.com"
        });

        userList.Users.Add(new UserResponse()
        {
            Id = Guid.NewGuid(),
            Name = "Arya",
            LastName = "Stark",
            Email = "SilentStark@gmail.com"
        });

        return userList;
    }
}
