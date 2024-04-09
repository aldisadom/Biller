using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceData;
using Contracts.Responses;
using Contracts.Responses.InvoiceData;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using WebAPI.SwaggerExamples.InvoiceData;

namespace WebAPI.Controllers;

/// <summary>
/// This is a invoice controller
/// </summary>
[ApiController]
[Route("[controller]")]
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
    /// Create invoice
    /// </summary>
    /// <returns>Invoice file name</returns>
    [HttpPut]
    [ProducesResponseType(typeof(InvoiceDataAddResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(InvoiceDataAddResponseExample))]
    [SwaggerRequestExample(typeof(InvoiceDataAddRequest), typeof(InvoiceDataAddRequestExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(InvoiceDataAddRequest invoiceData)
    {
        InvoiceDataModel invoiceDataModel = new()
        {
            SellerId = invoiceData.SellerId,
            CustomerId = invoiceData.CustomerId,
            UserId = invoiceData.UserId,
            DueDate = invoiceData.DueDate,
            Items = invoiceData.Items.Select(i => _mapper.Map<InvoiceItemModel>(i)).ToList(),

            Comments = invoiceData.Comments,
            CreatedDate = invoiceData.CreatedDate,
        };

        //if (invoiceData.CreatedDate is null)

        InvoiceDataAddResponse result = new()
        {
            Id = await _invoiceService.Add(invoiceDataModel),
        };
        return CreatedAtAction(nameof(Add), result);
    }
}
