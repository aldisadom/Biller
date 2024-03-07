using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.User;
using Contracts.Responses;
using Contracts.Responses.User;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using WebAPI.SwaggerExamples.User;

namespace WebAPI.Controllers;

/// <summary>
/// This is a user controller
/// </summary>
[ApiController]
[Route("[controller]")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="userService"></param>
    /// <param name="logger"></param>
    /// <param name="mapper"></param>
    public UserController(IUserService userService, ILogger<UserController> logger, IMapper mapper)
    {
        _userService = userService;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Login
    /// </summary>
    /// <param name="user">Users unique ID</param>
    /// <returns>taken</returns>
    [HttpGet("Login")]
    [ProducesResponseType(typeof(UserLoginResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(UserLoginResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromQuery] UserLoginRequest user)
    {
        UserModel userModel = _mapper.Map<UserModel>(user);

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

        UserResponse result = _mapper.Map<UserResponse>(user);

        return Ok(result);
    }

    /// <summary>
    /// Gets all users
    /// </summary>
    /// <returns>list of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(UserListResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(UserListResponseExample))]
    public async Task<IActionResult> Get()
    {
        IEnumerable<UserModel> users = await _userService.Get();

        UserListResponse result = new()
        {
            Users = users.Select(i => _mapper.Map<UserResponse>(i)).ToList()
        };

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
        UserModel userModel = _mapper.Map<UserModel>(user);

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
        UserModel userModel = _mapper.Map<UserModel>(user);

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