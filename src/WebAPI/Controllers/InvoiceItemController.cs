using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoMapper;
using Contracts.Requests.InvoiceItem;
using Contracts.Requests.InvoiceItem;
using Contracts.Responses;
using Contracts.Responses.InvoiceItem;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using WebAPI.SwaggerExamples.InvoiceItem;
using WebAPI.SwaggerExamples.InvoiceItem;

namespace WebAPI.Controllers;

/// <summary>
/// This is a invoiceItem controller
/// </summary>
[ApiController]
[Route("[controller]")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class InvoiceItemController : ControllerBase
{
    private readonly IInvoiceItemService _invoiceItemService;
    private readonly ILogger<InvoiceItemController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="invoiceItemService"></param>
    /// <param name="logger"></param>
    /// <param name="mapper"></param>
    public InvoiceItemController(IInvoiceItemService invoiceItemService, ILogger<InvoiceItemController> logger, IMapper mapper)
    {
        _invoiceItemService = invoiceItemService;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Get one invoiceItem
    /// </summary>
    /// <param name="id">Items unique ID</param>
    /// <returns>invoiceItem data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InvoiceItemResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(InvoiceItemResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        InvoiceItemModel invoiceItem = await _invoiceItemService.Get(id);

        InvoiceItemResponse result = _mapper.Map<InvoiceItemResponse>(invoiceItem);

        return Ok(result);
    }

    /// <summary>
    /// Gets all invoiceItems
    /// </summary>
    /// <param name="query">filtering parameters</param>
    /// <returns>list of invoiceItems</returns>
    [HttpGet]
    [ProducesResponseType(typeof(InvoiceItemListResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(InvoiceItemListResponseExample))]
    public async Task<IActionResult> Get([FromQuery]InvoiceItemGetRequest query)
    {
        IEnumerable<InvoiceItemModel> invoiceItems = await _invoiceItemService.Get(query);

        InvoiceItemListResponse result = new()
        {
            InvoiceItems = invoiceItems.Select(i => _mapper.Map<InvoiceItemResponse>(i)).ToList()
        };

        return Ok(result);
    }

    /// <summary>
    /// Add new invoiceItem
    /// </summary>
    /// <param name="invoiceItem">invoiceItem data to add</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(AddResponse), StatusCodes.Status201Created)]
    [SwaggerRequestExample(typeof(InvoiceItemAddRequest), typeof(InvoiceItemAddRequestExample))]
    public async Task<IActionResult> Add(InvoiceItemAddRequest invoiceItem)
    {
        InvoiceItemModel invoiceItemModel = _mapper.Map<InvoiceItemModel>(invoiceItem);

        AddResponse result = new()
        {
            Id = await _invoiceItemService.Add(invoiceItemModel),
        };
        return CreatedAtAction(nameof(Add), result);
    }

    /// <summary>
    /// Update invoiceItem
    /// </summary>
    /// <param name="invoiceItem">invoiceItem data to update</param>
    /// <returns></returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerRequestExample(typeof(InvoiceItemUpdateRequest), typeof(InvoiceItemUpdateRequestExample))]
    public async Task<IActionResult> Update(InvoiceItemUpdateRequest invoiceItem)
    {
        InvoiceItemModel invoiceItemModel = _mapper.Map<InvoiceItemModel>(invoiceItem);

        await _invoiceItemService.Update(invoiceItemModel);

        return NoContent();
    }

    /// <summary>
    /// Delete invoiceItem
    /// </summary>
    /// <param name="id">invoiceItem id to delete</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _invoiceItemService.Delete(id);

        return NoContent();
    }
}