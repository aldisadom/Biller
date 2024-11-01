using FluentValidation;
using FluentValidation.Results;

namespace Validators;

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
    public static void CheckValidation<T>(this IValidator<T> validator, T? data)
    {
        if (data is null)
            throw new ValidationException($"{typeof(T)} is not provided");

        ValidationResult validation = validator.Validate(data);

        if (!validation.IsValid)
        {
            string errorMessage = validation.ToString();

            throw new ValidationException(errorMessage);
        }
    }
}
