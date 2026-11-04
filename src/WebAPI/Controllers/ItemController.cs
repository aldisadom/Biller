using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Requests.Item;
using BillerContracts.Responses;
using BillerContracts.Responses.Item;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using Validators;
using WebAPI.SwaggerExamples.Item;

namespace WebAPI.Controllers;

/// <summary>
/// This is a item controller
/// </summary>
[ApiController]
[Route("items")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class ItemController : ControllerBase
{
    private readonly IItemService _itemService;
    private readonly ILogger<ItemController> _logger;
    private readonly IValidator<ItemAddRequest> _validatorAdd;
    private readonly IValidator<ItemUpdateRequest> _validatorUpdate;
    private readonly IValidator<ItemGetRequest> _validatorGet;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="itemService"></param>
    /// <param name="logger"></param>
    /// <param name="validatorAdd"></param>
    /// <param name="validatorUpdate"></param>
    /// <param name="validatorGet"></param>
    public ItemController(IItemService itemService, ILogger<ItemController> logger,
        IValidator<ItemAddRequest> validatorAdd, IValidator<ItemUpdateRequest> validatorUpdate,
        IValidator<ItemGetRequest> validatorGet)
    {
        _itemService = itemService;
        _logger = logger;

        _validatorAdd = validatorAdd;
        _validatorUpdate = validatorUpdate;
        _validatorGet = validatorGet;
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

        ItemResponse result = item.ToResponse();

        return Ok(result);
    }

    /// <summary>
    /// Gets one page of items (default page 1, 25 per page) and their total count
    /// </summary>
    /// <param name="query">filtering and paging parameters</param>
    /// <returns>one page of items</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ItemListResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ItemListResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get([FromQuery] ItemGetRequest query)
    {
        _validatorGet.CheckValidation(query);

        var (items, totalCount) = await _itemService.Get(query);

        ItemListResponse result = new ItemListResponse
        {
            Items = items.Select(i => i.ToResponse()).ToList()
        }.WithPaging(query, totalCount);

        return Ok(result);
    }

    /// <summary>
    /// Add new item
    /// </summary>
    /// <param name="item">item data to add</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(AddResponse), StatusCodes.Status201Created)]
    [SwaggerRequestExample(typeof(ItemAddRequest), typeof(ItemAddRequestExample))]
    public async Task<IActionResult> Add(ItemAddRequest item)
    {
        _validatorAdd.CheckValidation(item);
        ItemModel itemModel = item.ToModel();

        AddResponse result = new()
        {
            Id = await _itemService.Add(itemModel),
        };
        return CreatedAtAction(nameof(Add), result);
    }

    /// <summary>
    /// Update item
    /// </summary>
    /// <param name="item">item data to update</param>
    /// <returns></returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerRequestExample(typeof(ItemUpdateRequest), typeof(ItemUpdateRequestExample))]
    public async Task<IActionResult> Update(ItemUpdateRequest item)
    {
        _validatorUpdate.CheckValidation(item);
        ItemModel itemModel = item.ToModel();

        await _itemService.Update(itemModel);

        return NoContent();
    }

    /// <summary>
    /// Delete item
    /// </summary>
    /// <param name="id">item id to delete</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _itemService.Delete(id);

        return NoContent();
    }
}
