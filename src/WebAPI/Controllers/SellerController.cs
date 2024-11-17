using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Contracts.Requests.Seller;
using Contracts.Responses;
using Contracts.Responses.Seller;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using WebAPI.SwaggerExamples.Seller;

namespace WebAPI.Controllers;

/// <summary>
/// This is a Seller controller
/// </summary>
[ApiController]
[Route("sellers")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class SellerController : ControllerBase
{
    private readonly ISellerService _sellerService;
    private readonly ILogger<SellerController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="sellerService"></param>
    /// <param name="logger"></param>
    /// <param name="mapper"></param>
    public SellerController(ISellerService sellerService, ILogger<SellerController> logger, IMapper mapper)
    {
        _sellerService = sellerService;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Get one Seller
    /// </summary>
    /// <param name="id">Items unique ID</param>
    /// <returns>Seller data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SellerResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(SellerResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        SellerModel seller = await _sellerService.Get(id);

        SellerResponse result = _mapper.Map<SellerResponse>(seller);

        return Ok(result);
    }

    /// <summary>
    /// Gets all Sellers
    /// </summary>
    /// <param name="query">filtering parameters</param>
    /// <returns>list of Sellers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SellerListResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(SellerListResponseExample))]
    public async Task<IActionResult> Get([FromQuery] SellerGetRequest? query)
    {
        IEnumerable<SellerModel> sellers = await _sellerService.Get(query);

        SellerListResponse result = new()
        {
            Sellers = sellers.Select(i => _mapper.Map<SellerResponse>(i)).ToList()
        };

        return Ok(result);
    }

    /// <summary>
    /// Add new Seller
    /// </summary>
    /// <param name="seller">Seller data to add</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(AddResponse), StatusCodes.Status201Created)]
    [SwaggerRequestExample(typeof(SellerAddRequest), typeof(SellerAddRequestExample))]
    public async Task<IActionResult> Add(SellerAddRequest seller)
    {
        new SellerAddValidator().CheckValidation(seller);

        SellerModel sellerModel = _mapper.Map<SellerModel>(seller);

        AddResponse result = new()
        {
            Id = await _sellerService.Add(sellerModel),
        };
        return CreatedAtAction(nameof(Add), result);
    }

    /// <summary>
    /// Update Seller
    /// </summary>
    /// <param name="seller">Seller data to update</param>
    /// <returns></returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerRequestExample(typeof(SellerUpdateRequest), typeof(SellerUpdateRequestExample))]
    public async Task<IActionResult> Update(SellerUpdateRequest seller)
    {
        new SellerUpdateValidator().CheckValidation(seller);

        SellerModel sellerModel = _mapper.Map<SellerModel>(seller);

        await _sellerService.Update(sellerModel);

        return NoContent();
    }

    /// <summary>
    /// Delete Seller
    /// </summary>
    /// <param name="id">Seller id to delete</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _sellerService.Delete(id);

        return NoContent();
    }
}
