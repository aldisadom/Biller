using Application.Interfaces;
using Application.Models;
using Contracts.Requests;
using Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using WebAPI.SwaggerExamples.Item;

namespace WebAPI.Controllers;

/// <summary>
/// This is a item controller
/// </summary>
[ApiController]
[Route("v1/[controller]")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class ItemController : ControllerBase
{
    private readonly IItemService _itemService;
    private readonly ILogger<ItemController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="itemService"></param>
    /// <param name="logger"></param>
    public ItemController(IItemService itemService, ILogger<ItemController> logger)
    {
        _itemService = itemService;
        _logger = logger;
    }

    /// <summary>
    /// Get one item
    /// </summary>
    /// <param name="id">Items unique ID</param>
    /// <returns>item data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ItemResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ItemResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        ItemModel item = await _itemService.Get(id);

        ItemResponse result = new()
        {
            Id = item.Id,
            Name = item.Name,
            Price = item.Price,
            ShopId = item.ShopId,
        };

        return Ok(result);
    }

    /// <summary>
    /// Gets all items
    /// </summary>
    /// <returns>list of items</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ItemListResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ItemListResponseExample))]
    public async Task<IActionResult> Get()
    {
        IEnumerable<ItemModel> items = await _itemService.Get();

        ItemListResponse result = new();

        result.Items = items.Select(i => new ItemResponse()
        {
            Id = i.Id,
            Name = i.Name,
            Price = i.Price,
            ShopId = i.ShopId,
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Add new item
    /// </summary>
    /// <param name="item">item data to add</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(ItemAddResponse), StatusCodes.Status201Created)]
    [SwaggerRequestExample(typeof(ItemAddResponseExample), typeof(ItemAddRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(ItemAddResponseExample))]
    public async Task<IActionResult> Add(ItemAddRequest item)
    {
        ItemModel itemModel = new()
        {
            Name = item.Name,
            Price = item.Price,
            ShopId = item.ShopId,
        };

        ItemAddResponse result = new()
        {
            Id = await _itemService.Add(itemModel),
        };
        return CreatedAtAction(nameof(Add), result);
    }
}