using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoMapper;
using Contracts.Requests.InvoiceClient;
using Contracts.Requests.InvoiceItem;
using Contracts.Requests.InvoiceClient;
using Contracts.Responses;
using Contracts.Responses.InvoiceClient;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using WebAPI.SwaggerExamples.InvoiceClient;
using WebAPI.SwaggerExamples.InvoiceClient;

namespace WebAPI.Controllers;

/// <summary>
/// This is a invoiceClient controller
/// </summary>
[ApiController]
[Route("[controller]")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class InvoiceClientController : ControllerBase
{
    private readonly IInvoiceClientService _invoiceClientService;
    private readonly ILogger<InvoiceClientController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="invoiceClientService"></param>
    /// <param name="logger"></param>
    /// <param name="mapper"></param>
    public InvoiceClientController(IInvoiceClientService invoiceClientService, ILogger<InvoiceClientController> logger, IMapper mapper)
    {
        _invoiceClientService = invoiceClientService;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Get one invoiceClient
    /// </summary>
    /// <param name="id">Items unique ID</param>
    /// <returns>invoiceClient data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InvoiceClientResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(InvoiceClientResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        InvoiceClientModel invoiceClient = await _invoiceClientService.Get(id);

        InvoiceClientResponse result = _mapper.Map<InvoiceClientResponse>(invoiceClient);

        return Ok(result);
    }

    /// <summary>
    /// Gets all invoiceClients
    /// </summary>
    /// <param name="query">filtering parameters</param>
    /// <returns>list of invoiceClients</returns>
    [HttpGet]
    [ProducesResponseType(typeof(InvoiceClientListResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(InvoiceClientListResponseExample))]
    public async Task<IActionResult> Get([FromQuery] InvoiceClientGetRequest query)
    {
        IEnumerable<InvoiceClientModel> invoiceClients = await _invoiceClientService.Get(query);

        InvoiceClientListResponse result = new()
        {
            InvoiceClients = invoiceClients.Select(i => _mapper.Map<InvoiceClientResponse>(i)).ToList()
        };

        return Ok(result);
    }

    /// <summary>
    /// Add new invoiceClient
    /// </summary>
    /// <param name="invoiceClient">invoiceClient data to add</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(AddResponse), StatusCodes.Status201Created)]
    [SwaggerRequestExample(typeof(InvoiceClientAddRequest), typeof(InvoiceClientAddRequestExample))]
    public async Task<IActionResult> Add(InvoiceClientAddRequest invoiceClient)
    {
        InvoiceClientModel invoiceClientModel = _mapper.Map<InvoiceClientModel>(invoiceClient);

        AddResponse result = new()
        {
            Id = await _invoiceClientService.Add(invoiceClientModel),
        };
        return CreatedAtAction(nameof(Add), result);
    }

    /// <summary>
    /// Update invoiceClient
    /// </summary>
    /// <param name="invoiceClient">invoiceClient data to update</param>
    /// <returns></returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerRequestExample(typeof(InvoiceClientUpdateRequest), typeof(InvoiceClientUpdateRequestExample))]
    public async Task<IActionResult> Update(InvoiceClientUpdateRequest invoiceClient)
    {
        InvoiceClientModel invoiceClientModel = _mapper.Map<InvoiceClientModel>(invoiceClient);

        await _invoiceClientService.Update(invoiceClientModel);

        return NoContent();
    }

    /// <summary>
    /// Delete invoiceClient
    /// </summary>
    /// <param name="id">invoiceClient id to delete</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _invoiceClientService.Delete(id);

        return NoContent();
    }
}