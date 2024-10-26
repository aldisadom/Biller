using Contracts.Responses;
using Domain.Exceptions;
using Domain.Models;
using Newtonsoft.Json;
using System.Data.Common;
using System.Linq;
using System.Security;
using ValidationException = FluentValidation.ValidationException;

namespace WebAPI.Middleware;

/// <summary>
/// Error handling middleware
/// </summary>
public class ErrorChecking
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorChecking> _logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="next"></param>
    /// <param name="logger"></param>
    public ErrorChecking(RequestDelegate next, ILogger<ErrorChecking> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception e)
        {
            string message;
            string extendedMessage;
            Exception exception;
            int statusCode;

            switch (e)
            {
                case ValidationException:
                    message = "Validation failure";
                    extendedMessage = e.Message;
                    exception = e;
                    statusCode = StatusCodes.Status400BadRequest;
                    break;

                case UnauthorizedAccessException:
                    message = "Unauthorized";
                    extendedMessage = e.Message;
                    exception = e;
                    statusCode = StatusCodes.Status401Unauthorized;
                    break;

                case NotImplementedException:
                    message = "Not implemented";
                    extendedMessage = e.Message;
                    exception = e;
                    statusCode = StatusCodes.Status501NotImplemented;
                    break;

                case SecurityException:
                    message = "Authentication error";
                    extendedMessage = e.Message;
                    exception = e;
                    statusCode = StatusCodes.Status401Unauthorized;
                    break;

                case NullReferenceException:
                    message = "Null reference caught";
                    extendedMessage = e.Message;
                    exception = e;
                    statusCode = StatusCodes.Status404NotFound;
                    break;

                case ArgumentException:
                    message = "Argument is incorrect";
                    extendedMessage = e.Message;
                    exception = e;
                    statusCode = StatusCodes.Status400BadRequest;
                    break;

                case NotFoundException:
                    message = "Entity not found";
                    extendedMessage = e.Message;
                    exception = e;
                    statusCode = StatusCodes.Status404NotFound;
                    break;

                case Npgsql.PostgresException:

                    string constrain = ((Npgsql.PostgresException)e).ConstraintName ?? "";

                    if (e.Message.Contains("duplicate key value violates unique constraint"))
                    {
                        message = "Validation failure";
                        extendedMessage = "Key is already used: " + constrain.Split("_")[1];
                        exception = e;
                        statusCode = StatusCodes.Status400BadRequest;
                    }
                    else if (e.Message.Contains("update or delete") && e.Message.Contains("violates foreign key constraint"))
                    {                        
                        message = "Validation failure";
                        extendedMessage = "Can not delete (please clear all dependants) or update (item not found): " + constrain.Split("fk_")[1];
                        exception = e;
                        statusCode = StatusCodes.Status400BadRequest;                        
                    }
                    else if (e.Message.Contains("insert or update") && e.Message.Contains("violates foreign key constraint"))
                    {
                        message = "Validation failure";
                        extendedMessage = "Key does not exist: " + constrain.Split("fk_")[1];
                        exception = e;
                        statusCode = StatusCodes.Status400BadRequest;
                    }
                    else
                    {
                        message = "Database error";
                        extendedMessage = e.Message;
                        exception = e;
                        statusCode = StatusCodes.Status500InternalServerError;
                    }
                    break;

                case DbException:
                    message = "Database error";
                    extendedMessage = e.Message;
                    exception = e;
                    statusCode = StatusCodes.Status500InternalServerError;
                    break;

                default:
                    message = "General error";
                    extendedMessage = e.Message;
                    exception = e;
                    statusCode = StatusCodes.Status500InternalServerError;
                    break;
            }

            ErrorModel errorMessage = new(message, extendedMessage, statusCode, e);
            await UpdateContextAndLog(errorMessage, context);
        }
    }

    private async Task UpdateContextAndLog(ErrorModel errorMessage, HttpContext context)
    {
        context.Response.StatusCode = errorMessage.StatusCode;

        _logger.LogError("{Message}", JsonConvert.SerializeObject(errorMessage));

        ErrorResponse response = new()
        {
            Message = errorMessage.Message,
            ExtendedMessage = errorMessage.ExtendedMessage
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
