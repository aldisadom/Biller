using Contracts.Responses;
using Domain.Exceptions;
using Domain.Models;
using FluentValidation;
using Newtonsoft.Json;
using System.Data.Common;
using System.Security;
using ValidationException = FluentValidation.ValidationException;
using ValidationResult = FluentValidation.Results.ValidationResult;

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
            string exceptionMessage;
            int statusCode;

            switch (e)
            {
                case ValidationException:
                    message = "Validation failure";
                    extendedMessage = e.Message;
                    exceptionMessage = e.Message;
                    statusCode = StatusCodes.Status400BadRequest;
                    break;

                case UnauthorizedAccessException:
                    message = "Unauthorized";
                    extendedMessage = e.Message;
                    exceptionMessage = e.Message;
                    statusCode = StatusCodes.Status401Unauthorized;
                    break;

                case NotImplementedException:
                    message = "Not implemented";
                    extendedMessage = e.Message;
                    exceptionMessage = e.Message;
                    statusCode = StatusCodes.Status501NotImplemented;
                    break;

                case SecurityException:
                    message = "Authentication error";
                    extendedMessage = e.Message;
                    exceptionMessage = e.Message;
                    statusCode = StatusCodes.Status401Unauthorized;
                    break;

                case NullReferenceException:
                    message = "Null reference caught";
                    extendedMessage = e.Message;
                    exceptionMessage = e.Message;
                    statusCode = StatusCodes.Status404NotFound;
                    break;

                case ArgumentException:
                    message = "Argument is incorrect";
                    extendedMessage = e.Message;
                    exceptionMessage = e.Message;
                    statusCode = StatusCodes.Status400BadRequest;
                    break;

                case NotFoundException:
                    message = "Entity not found";
                    extendedMessage = e.Message;
                    exceptionMessage = e.Message;
                    statusCode = StatusCodes.Status404NotFound;
                    break;

                case Npgsql.PostgresException:
                    message = "Database error";
                    extendedMessage = e.Message;
                    exceptionMessage = e.Message;
                    statusCode = StatusCodes.Status500InternalServerError;
                    break;

                case DbException:
                    message = "Database error";
                    extendedMessage = e.Message;
                    exceptionMessage = e.Message;
                    statusCode = StatusCodes.Status500InternalServerError;
                    break;

                default:
                    message = "General error";
                    extendedMessage = e.Message;
                    exceptionMessage = e.Message;
                    statusCode = StatusCodes.Status500InternalServerError;
                    break;
            }

            ErrorModel errorMessage = new(message, exceptionMessage, exceptionMessage, statusCode, e);
            await UpdateContextAndLog(errorMessage, context);
        }
    }

    private async Task UpdateContextAndLog(ErrorModel errorMessage, HttpContext context)
    {
        context.Response.StatusCode = errorMessage.StatusCode;

        _logger.LogError("{Message}", JsonConvert.SerializeObject(errorMessage));

        ErrorResponse response = new()
        {
            Message = errorMessage.Message
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}


/// <summary>
/// Fluent validation extension to throw exception when not valid
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Check and throw all errors
    /// </summary>
    /// <param name="validator"></param>
    /// <exception cref="ValidationException"></exception>
    public static void CheckValidation<T>(this AbstractValidator<T> validator, T data)
    {
        ValidationResult validation = validator.Validate(data);

        if (!validation.IsValid)
        {
            string errorMessage = validation.ToString();

            throw new ValidationException(errorMessage);
        }
    }
}
