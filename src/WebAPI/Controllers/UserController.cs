using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Requests.User;
using BillerContracts.Responses;
using BillerContracts.Responses.User;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using Validators;
using WebAPI.SwaggerExamples.User;

namespace WebAPI.Controllers;

/// <summary>
/// This is a user controller
/// </summary>
[ApiController]
[Route("users")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;
    private readonly IValidator<UserAddRequest> _validatorAdd;
    private readonly IValidator<UserUpdateRequest> _validatorUpdate;
    private readonly IValidator<UserLoginRequest> _validatorLogin;
    private readonly IValidator<UserGetRequest> _validatorGet;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="userService"></param>
    /// <param name="logger"></param>
    /// <param name="validatorAdd"></param>
    /// <param name="validatorUpdate"></param>
    /// <param name="validatorLogin"></param>
    /// <param name="validatorGet"></param>
    public UserController(IUserService userService, ILogger<UserController> logger,
        IValidator<UserAddRequest> validatorAdd, IValidator<UserUpdateRequest> validatorUpdate, IValidator<UserLoginRequest> validatorLogin,
        IValidator<UserGetRequest> validatorGet)
    {
        _userService = userService;
        _logger = logger;

        _validatorAdd = validatorAdd;
        _validatorUpdate = validatorUpdate;
        _validatorLogin = validatorLogin;
        _validatorGet = validatorGet;
    }

    /// <summary>
    /// Login
    /// </summary>
    /// <param name="user">Users unique ID</param>
    /// <returns>taken</returns>
    [HttpPost("Login")]
    [ProducesResponseType(typeof(UserLoginResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(UserLoginResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(UserLoginRequest user)
    {
        _validatorLogin.CheckValidation(user);

        UserModel userModel = user.ToModel();

        UserLoginResponse result = new()
        {
            Token = await _userService.Login(userModel)
        };

        return Ok(result);
    }

    /// <summary>
    /// Get one user
    /// </summary>
    /// <param name="id">Users unique ID</param>
    /// <returns>user data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(UserResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        UserModel user = await _userService.Get(id);

        UserResponse result = user.ToResponse();

        return Ok(result);
    }

    /// <summary>
    /// Gets all users
    /// </summary>
    /// <param name="query">paging parameters</param>
    /// <returns>list of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(UserListResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(UserListResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get([FromQuery] UserGetRequest query)
    {
        _validatorGet.CheckValidation(query);

        var (users, totalCount) = await _userService.Get(query);

        UserListResponse result = new UserListResponse
        {
            Users = users.Select(u => u.ToResponse()).ToList()
        }.WithPaging(query, totalCount);

        return Ok(result);
    }

    /// <summary>
    /// Add new user
    /// </summary>
    /// <param name="user">user data to add</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(AddResponse), StatusCodes.Status201Created)]
    [SwaggerRequestExample(typeof(UserAddRequest), typeof(UserAddRequestExample))]
    public async Task<IActionResult> Add(UserAddRequest user)
    {
        _validatorAdd.CheckValidation(user);

        UserModel userModel = user.ToModel();

        AddResponse result = new()
        {
            Id = await _userService.Add(userModel),
        };
        return CreatedAtAction(nameof(Add), result);
    }

    /// <summary>
    /// Update user
    /// </summary>
    /// <param name="user">user data to update</param>
    /// <returns></returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerRequestExample(typeof(UserUpdateRequest), typeof(UserUpdateRequestExample))]
    public async Task<IActionResult> Update(UserUpdateRequest user)
    {
        _validatorUpdate.CheckValidation(user);

        UserModel userModel = user.ToModel();

        await _userService.Update(userModel);

        return NoContent();
    }

    /// <summary>
    /// Delete user
    /// </summary>
    /// <param name="id">user id to delete</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _userService.Delete(id);

        return NoContent();
    }
}
