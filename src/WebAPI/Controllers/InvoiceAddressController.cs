using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceAddress;
using Contracts.Responses;
using Contracts.Responses.InvoiceAddress;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using WebAPI.SwaggerExamples.InvoiceClient;

namespace WebAPI.Controllers;

/// <summary>
/// This is a invoiceAddress controller
/// </summary>
[ApiController]
[Route("[controller]")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class InvoiceAddressController : ControllerBase
{
    private readonly IInvoiceAddressService _invoiceAddressService;
    private readonly ILogger<InvoiceAddressController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="invoiceAddressService"></param>
    /// <param name="logger"></param>
    /// <param name="mapper"></param>
    public InvoiceAddressController(IInvoiceAddressService invoiceAddressService, ILogger<InvoiceAddressController> logger, IMapper mapper)
    {
        _invoiceAddressService = invoiceAddressService;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Get one invoiceAddress
    /// </summary>
    /// <param name="id">Items unique ID</param>
    /// <returns>invoiceAddress data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InvoiceAddressResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(InvoiceAddressResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        InvoiceAddressModel invoiceAddress = await _invoiceAddressService.Get(id);

        InvoiceAddressResponse result = _mapper.Map<InvoiceAddressResponse>(invoiceAddress);

        return Ok(result);
    }

    /// <summary>
    /// Gets all invoiceAddresss
    /// </summary>
    /// <param name="query">filtering parameters</param>
    /// <returns>list of invoiceAddresss</returns>
    [HttpGet]
    [ProducesResponseType(typeof(InvoiceAddressListResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(InvoiceAddressListResponseExample))]
    public async Task<IActionResult> Get([FromQuery] InvoiceAddressGetRequest query)
    {
        IEnumerable<InvoiceAddressModel> invoiceAddresss = await _invoiceAddressService.Get(query);

        InvoiceAddressListResponse result = new()
        {
            InvoiceAddresss = invoiceAddresss.Select(i => _mapper.Map<InvoiceAddressResponse>(i)).ToList()
        };

        return Ok(result);
    }

    /// <summary>
    /// Add new invoiceAddress
    /// </summary>
    /// <param name="invoiceAddress">invoiceAddress data to add</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(AddResponse), StatusCodes.Status201Created)]
    [SwaggerRequestExample(typeof(InvoiceAddressAddRequest), typeof(InvoiceAddressAddRequestExample))]
    public async Task<IActionResult> Add(InvoiceAddressAddRequest invoiceAddress)
    {
        InvoiceAddressModel invoiceAddressModel = _mapper.Map<InvoiceAddressModel>(invoiceAddress);

        AddResponse result = new()
        {
            Id = await _invoiceAddressService.Add(invoiceAddressModel),
        };
        return CreatedAtAction(nameof(Add), result);
    }

    /// <summary>
    /// Update invoiceAddress
    /// </summary>
    /// <param name="invoiceAddress">invoiceAddress data to update</param>
    /// <returns></returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerRequestExample(typeof(InvoiceAddressUpdateRequest), typeof(InvoiceAddressUpdateRequestExample))]
    public async Task<IActionResult> Update(InvoiceAddressUpdateRequest invoiceAddress)
    {
        InvoiceAddressModel invoiceAddressModel = _mapper.Map<InvoiceAddressModel>(invoiceAddress);

        await _invoiceAddressService.Update(invoiceAddressModel);

        return NoContent();
    }

    /// <summary>
    /// Delete invoiceAddress
    /// </summary>
    /// <param name="id">invoiceAddress id to delete</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _invoiceAddressService.Delete(id);

        return NoContent();
    }
}