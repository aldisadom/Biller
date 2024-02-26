using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceItem;
using Contracts.Responses;
using Contracts.Responses.InvoiceItem;
using Contracts.Responses.User;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using WebAPI.SwaggerExamples.Item;

namespace WebAPI.Controllers;

/// <summary>
/// This is a invoiceItem controller
/// </summary>
[ApiController]
[Route("v1/[controller]")]
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
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ItemResponseExample))]
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
    /// <returns>list of invoiceItems</returns>
    [HttpGet]
    [ProducesResponseType(typeof(InvoiceItemListResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ItemListResponseExample))]
    public async Task<IActionResult> Get()
    {
        IEnumerable<InvoiceItemModel> invoiceItems = await _invoiceItemService.Get();

        InvoiceItemListResponse result = new();

        result.InvoiceItems = invoiceItems.Select(i => _mapper.Map<InvoiceItemResponse>(i)).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Add new invoiceItem
    /// </summary>
    /// <param name="invoiceItem">invoiceItem data to add</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(InvoiceItemAddResponse), StatusCodes.Status201Created)]
    [SwaggerRequestExample(typeof(ItemAddResponseExample), typeof(ItemAddRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(ItemAddResponseExample))]
    public async Task<IActionResult> Add(InvoiceItemAddRequest invoiceItem)
    {
        InvoiceItemModel invoiceItemModel = _mapper.Map<InvoiceItemModel>(invoiceItem);
        
        InvoiceItemAddResponse result = new()
        {
            Id = await _invoiceItemService.Add(invoiceItemModel),
        };
        return CreatedAtAction(nameof(Add), result);
    }
}