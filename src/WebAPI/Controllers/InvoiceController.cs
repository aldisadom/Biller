using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using Contracts.Requests.Invoice;
using Contracts.Responses;
using Contracts.Responses.Invoice;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using Validators;
using Validators.Invoice;
using WebAPI.SwaggerExamples.Invoice;

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

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="invoiceService"></param>
    /// <param name="logger"></param>
    public InvoiceController(IInvoiceService invoiceService, ILogger<InvoiceController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Get one invoice
    /// </summary>
    /// <param name="id">Invoices unique ID</param>
    /// <returns>invoice data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(InvoiceResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        InvoiceModel invoice = await _invoiceService.Get(id);

        InvoiceResponse result = invoice.ToResponse();

        return Ok(result);
    }

    /// <summary>
    /// Gets all invoices
    /// </summary>
    /// <param name="query">filtering parameters</param>
    /// <returns>list of invoices</returns>
    [HttpGet]
    [ProducesResponseType(typeof(InvoiceListResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(InvoiceListResponseExample))]
    public async Task<IActionResult> Get([FromQuery] InvoiceGetRequest? query)
    {
        IEnumerable<InvoiceModel> invoicesData = await _invoiceService.Get(query);

        InvoiceListResponse result = new()
        {
            Invoices = invoicesData.Select(i => i.ToResponse()).ToList()
        };

        return Ok(result);
    }

    /// <summary>
    /// Create invoice
    /// </summary>
    /// <returns>Invoice file name</returns>
    [HttpPost]
    [SwaggerRequestExample(typeof(InvoiceAddRequest), typeof(InvoiceAddRequestExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(InvoiceAddRequest invoiceDataRequest)
    {
        new InvoiceAddValidator().CheckValidation(invoiceDataRequest);
        InvoiceModel invoiceData = invoiceDataRequest.ToModel();

        AddResponse result = new()
        {
            Id = await _invoiceService.Add(invoiceData),
        };
        return CreatedAtAction(nameof(Add), result);
    }

    /// <summary>
    /// Get invoice pdf
    /// </summary>
    /// <param name="query">Invoices ID and language selection</param>
    /// <returns>invoice pdf</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(InvoiceGenerateRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GeneratePDF([FromQuery] InvoiceGenerateRequest query)
    {
        new InvoiceGenerateValidator().CheckValidation(query);
        FileStream file = await _invoiceService.GeneratePDF(query.Id, query.LanguageCode, query.DocumentType);

        string fileName = Path.GetFileName(file.Name);

        return File(file, "application/pdf", fileName);
    }

    /// <summary>
    /// Update invoice
    /// </summary>
    /// <param name="invoice">invoice data to update</param>
    /// <returns></returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerRequestExample(typeof(InvoiceUpdateRequest), typeof(InvoiceUpdateRequestExample))]
    public async Task<IActionResult> Update(InvoiceUpdateRequest invoice)
    {
        new InvoiceUpdateValidator().CheckValidation(invoice);
        InvoiceModel invoiceData = invoice.ToModel();

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


    ///<summary>
    /// Updates the status of an invoice.
    /// </summary>
    /// <param name="invoice">The invoice status update request.</param>
    /// <returns>No content if the update is successful.</returns>
    [HttpPut("update_status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerRequestExample(typeof(InvoiceUpdateStatusRequest), typeof(InvoiceUpdateStatusRequestExample))]
    public async Task<IActionResult> UpdateStatus(InvoiceUpdateStatusRequest invoice)
    {
        await _invoiceService.UpdateStatus(invoice);

        return NoContent();
    }
}
