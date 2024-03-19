using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.InvoiceData;
using Contracts.Responses;
using Contracts.Responses.Item;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using WebAPI.SwaggerExamples.InvoiceData;
using WebAPI.SwaggerExamples.Item;

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
    [ProducesResponseType(typeof(ItemResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ItemResponseExample))]
    [SwaggerRequestExample(typeof(InvoiceDataGenerateRequest), typeof(InvoiceDataGenerateRequestExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Generate(InvoiceDataGenerateRequest invoiceData)
    {
        InvoiceDataModel invoiceDataModel = _mapper.Map<InvoiceDataModel>(invoiceData);

        await _invoiceService.GeneratePDF(invoiceDataModel);

        return Ok();
    }
}
