using FluentValidation;
using FluentValidation.Results;

namespace Contracts.Validations;

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
