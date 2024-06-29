using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoMapper;
using Contracts.Requests.InvoiceData;
using Contracts.Responses;
using Contracts.Responses.InvoiceData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Swashbuckle.AspNetCore.Filters;
using WebAPI.SwaggerExamples.InvoiceData;

namespace WebAPI.Controllers;

/// <summary>
/// This is a invoice controller
/// </summary>
[ApiController]
[Route("invoices")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoiceController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="invoiceService"></param>
    /// <param name="logger"></param>
    /// <param name="mapper"></param>
    public InvoiceController(IInvoiceService invoiceService, ILogger<InvoiceController> logger, IMapper mapper)
    {
        _invoiceService = invoiceService;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Get one invoice
    /// </summary>
    /// <param name="id">Invoices unique ID</param>
    /// <returns>invoice data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InvoiceDataResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(InvoiceDataResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        InvoiceDataModel invoice = await _invoiceService.Get(id);

        InvoiceDataResponse result = _mapper.Map<InvoiceDataResponse>(invoice);

        return Ok(result);
    }

    /// <summary>
    /// Gets all invoices
    /// </summary>
    /// <param name="query">filtering parameters</param>
    /// <returns>list of invoices</returns>
    [HttpGet]
    [ProducesResponseType(typeof(InvoiceDataListResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(InvoiceDataListResponseExample))]
    public async Task<IActionResult> Get([FromQuery] InvoiceDataGetRequest query)
    {
        IEnumerable<InvoiceDataModel> invoicesData = await _invoiceService.Get(query);

        InvoiceDataListResponse result = new()
        {
            Invoices = invoicesData.Select(i => _mapper.Map<InvoiceDataResponse>(i)).ToList()
        };

        return Ok(result);
    }

    /// <summary>
    /// Create invoice
    /// </summary>
    /// <returns>Invoice file name</returns>
    [HttpPost]
    [SwaggerRequestExample(typeof(InvoiceDataAddRequest), typeof(InvoiceDataAddRequestExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(InvoiceDataAddRequest invoiceDataRequest)
    {
        InvoiceDataModel invoiceData = new()
        {
            Seller = new SellerModel{
                Id = invoiceDataRequest.SellerId 
            },
            Customer = new CustomerModel
            {
                Id = invoiceDataRequest.CustomerId
            },
            User = new UserModel
            {
                Id = invoiceDataRequest.UserId
            },
            DueDate = invoiceDataRequest.DueDate,
            Items = invoiceDataRequest.Items.Select(i => _mapper.Map<InvoiceItemModel>(i)).ToList(),

            Comments = invoiceDataRequest.Comments,
            CreatedDate = invoiceDataRequest.CreatedDate,
        };

        AddResponse result = new()
        {
            Id = await _invoiceService.Add(invoiceData),
        };
        return CreatedAtAction(nameof(Add), result);
    }

    /// <summary>
    /// Get invoice pdf
    /// </summary>
    /// <param name="id">Invoices unique ID</param>
    /// <returns>invoice pdf</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(InvoiceDataResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(InvoiceDataResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GeneratePDF(Guid id)
    {
        await _invoiceService.GeneratePDF(id);

        return Ok();
    }

    /// <summary>
    /// Update invoice
    /// </summary>
    /// <param name="invoice">invoice data to update</param>
    /// <returns></returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerRequestExample(typeof(InvoiceDataUpdateRequest), typeof(InvoiceDataUpdateRequestExample))]
    public async Task<IActionResult> Update(InvoiceDataUpdateRequest invoice)
    {
        InvoiceDataModel invoiceData = _mapper.Map<InvoiceDataModel>(invoice);

        await _invoiceService.Update(invoiceData);

        return NoContent();
    }

    /// <summary>
    /// Delete invoice
    /// </summary>
    /// <param name="id">invoice id to delete</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _invoiceService.Delete(id);

        return NoContent();
    }
}
