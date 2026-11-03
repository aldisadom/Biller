using BillerContracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace WebAPI.Middleware;

/// <summary>
/// Model binding errors answered and logged like the validation errors in <see cref="ErrorChecking"/>
/// </summary>
public static class ModelStateErrors
{
    /// <summary>
    /// Shown for an error that has only an exception, so its text doesn't reach the client
    /// </summary>
    public const string InvalidInputMessage = "The input was not valid.";

    /// <summary>
    /// 400 with "Validation failure" and every binding error, one per line
    /// </summary>
    /// <param name="context">action whose model state is invalid</param>
    /// <returns></returns>
    public static IActionResult ToErrorResponse(ActionContext context)
    {
        IEnumerable<string> errors = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? InvalidInputMessage : e.ErrorMessage);

        ErrorResponse response = new()
        {
            Message = "Validation failure",
            ExtendedMessage = string.Join(Environment.NewLine, errors)
        };

        ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(ModelStateErrors));
        logger.LogError("{Message}", JsonConvert.SerializeObject(response));

        return new BadRequestObjectResult(response);
    }
}
