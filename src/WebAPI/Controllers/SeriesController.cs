using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using BillerContracts.Enums;
using BillerContracts.Requests.Series;
using BillerContracts.Responses;
using BillerContracts.Responses.Series;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using Validators;
using WebAPI.SwaggerExamples.Series;

namespace WebAPI.Controllers;

/// <summary>
/// This is a Series controller
/// </summary>
[ApiController]
[Route("series")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class SeriesController : ControllerBase
{
    private readonly ISeriesService _seriesService;
    private readonly ILogger<SeriesController> _logger;
    private readonly IValidator<SeriesAddRequest> _validatorAdd;
    private readonly IValidator<SeriesUpdateRequest> _validatorUpdate;
    private readonly IValidator<SeriesGetRequest> _validatorGet;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="seriesService"></param>
    /// <param name="logger"></param>
    /// <param name="validatorAdd"></param>
    /// <param name="validatorUpdate"></param>
    /// <param name="validatorGet"></param>
    public SeriesController(ISeriesService seriesService, ILogger<SeriesController> logger,
        IValidator<SeriesAddRequest> validatorAdd, IValidator<SeriesUpdateRequest> validatorUpdate,
        IValidator<SeriesGetRequest> validatorGet)
    {
        _seriesService = seriesService;
        _logger = logger;

        _validatorAdd = validatorAdd;
        _validatorUpdate = validatorUpdate;
        _validatorGet = validatorGet;
    }

    /// <summary>
    /// Get one Series
    /// </summary>
    /// <param name="id">Series unique ID</param>
    /// <returns>Series data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SeriesResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(SeriesResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        SeriesModel series = await _seriesService.Get(id);

        SeriesResponse result = series.ToResponse();

        return Ok(result);
    }

    /// <summary>
    /// Gets all Series
    /// </summary>
    /// <param name="query">filtering parameters</param>
    /// <returns>list of Series</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SeriesListResponse), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(SeriesListResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get([FromQuery] SeriesGetRequest? query)
    {
        if (query is not null)
            _validatorGet.CheckValidation(query);

        var (series, totalCount) = await _seriesService.Get(query);

        SeriesListResponse result = new()
        {
            Series = series.Select(s => s.ToResponse()).ToList(),
            TotalCount = totalCount,
            Page = query?.Page ?? 1,
            PageSize = query?.PageSize ?? PageSize.p25
        };

        return Ok(result);
    }

    /// <summary>
    /// Add new Series
    /// </summary>
    /// <param name="series">Series data to add</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(AddResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [SwaggerRequestExample(typeof(SeriesAddRequest), typeof(SeriesAddRequestExample))]
    public async Task<IActionResult> Add(SeriesAddRequest series)
    {
        _validatorAdd.CheckValidation(series);
        SeriesModel seriesModel = series.ToModel();

        AddResponse result = new()
        {
            Id = await _seriesService.Add(seriesModel),
        };
        return CreatedAtAction(nameof(Add), result);
    }

    /// <summary>
    /// Update Series
    /// </summary>
    /// <param name="series">Series data to update</param>
    /// <returns></returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [SwaggerRequestExample(typeof(SeriesUpdateRequest), typeof(SeriesUpdateRequestExample))]
    public async Task<IActionResult> Update(SeriesUpdateRequest series)
    {
        _validatorUpdate.CheckValidation(series);
        SeriesModel seriesModel = series.ToModel();

        await _seriesService.Update(seriesModel);

        return NoContent();
    }

    /// <summary>
    /// Delete Series. A series used by customers or invoices can't be deleted.
    /// </summary>
    /// <param name="id">Series id to delete</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _seriesService.Delete(id);

        return NoContent();
    }
}
