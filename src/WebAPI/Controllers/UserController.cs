﻿using Application;
using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.User;
using Contracts.Responses;
using Contracts.Responses.User;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using System.Linq;
using WebAPI.SwaggerExamples.Item;

namespace WebAPI.Controllers;

/// <summary>
/// This is a user controller
/// </summary>
[ApiController]
[Route("v1/[controller]")]
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
    public UserController(IUserService userService, ILogger<UserController> logger, IMapper mapper)
    {
        _userService = userService;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Get one user
    /// </summary>
    /// <param name="id">Items unique ID</param>
    /// <returns>user data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ItemResponseExample))]
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
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ItemListResponseExample))]
    public async Task<IActionResult> Get()
    {
        IEnumerable<UserModel> users = await _userService.Get();

        UserListResponse result = new();

        result.Users = users.Select(i => _mapper.Map<UserResponse>(i)).ToList();
        
        return Ok(result);
    }

    /// <summary>
    /// Add new user
    /// </summary>
    /// <param name="user">user data to add</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserAddResponse), StatusCodes.Status201Created)]
//    [SwaggerRequestExample(typeof(ItemAddResponseExample), typeof(ItemAddRequestExample))]
 //   [SwaggerResponseExample(StatusCodes.Status201Created, typeof(ItemAddResponseExample))]
    public async Task<IActionResult> Add(UserAddRequest user)
    {
        UserModel userModel = _mapper.Map<UserModel>(user);

        UserAddResponse result = new()
        {
            Id = await _userService.Add(userModel),
        };
        return CreatedAtAction(nameof(Add), result);
    }
}