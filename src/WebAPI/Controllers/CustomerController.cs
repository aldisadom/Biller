using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.Customer;
using Contracts.Responses;
using Contracts.Responses.Customer;
using Contracts.Validations;
using Contracts.Validations.Customer;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using WebAPI.SwaggerExamples.Customer;

namespace WebAPI.Controllers;

/// <summary>
/// This is a Customer controller
/// </summary>
[ApiController]
[Route("customers")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomerController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="customerService"></param>
    /// <param name="logger"></param>
    /// <param name="mapper"></param>
    public CustomerController(ICustomerService customerService, ILogger<CustomerController> logger, IMapper mapper)
    {
        _customerService = customerService;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Get one Customer
    /// </summary>
    /// <param name="id">Items unique ID</param>
    /// <returns>Customer data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(CustomerResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        CustomerModel customer = await _customerService.Get(id);

        CustomerResponse result = _mapper.Map<CustomerResponse>(customer);

        return Ok(result);
    }

    /// <summary>
    /// Gets all Customers
    /// </summary>
    /// <param name="query">filtering parameters</param>
    /// <returns>list of Customers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(CustomerListResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(CustomerListResponseExample))]
    public async Task<IActionResult> Get([FromQuery] CustomerGetRequest? query)
    {
        IEnumerable<CustomerModel> customers = await _customerService.Get(query);

        CustomerListResponse result = new()
        {
            Customers = customers.Select(i => _mapper.Map<CustomerResponse>(i)).ToList()
        };

        return Ok(result);
    }

    /// <summary>
    /// Add new Customer
    /// </summary>
    /// <param name="customer">Customer data to add</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(AddResponse), StatusCodes.Status201Created)]
    [SwaggerRequestExample(typeof(CustomerAddRequest), typeof(CustomerAddRequestExample))]
    public async Task<IActionResult> Add(CustomerAddRequest customer)
    {
        new CustomerAddValidator().CheckValidation(customer);
        CustomerModel customerModel = _mapper.Map<CustomerModel>(customer);

        AddResponse result = new()
        {
            Id = await _customerService.Add(customerModel),
        };
        return CreatedAtAction(nameof(Add), result);
    }

    /// <summary>
    /// Update Customer
    /// </summary>
    /// <param name="customer">Customer data to update</param>
    /// <returns></returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerRequestExample(typeof(CustomerUpdateRequest), typeof(CustomerUpdateRequestExample))]
    public async Task<IActionResult> Update(CustomerUpdateRequest customer)
    {
        new CustomerUpdateValidator().CheckValidation(customer);
        CustomerModel customerModel = _mapper.Map<CustomerModel>(customer);

        await _customerService.Update(customerModel);

        return NoContent();
    }

    /// <summary>
    /// Delete Customer
    /// </summary>
    /// <param name="id">Customer id to delete</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _customerService.Delete(id);

        return NoContent();
    }
}
