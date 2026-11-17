using Application.Helpers;
using Application.Interfaces;
using FluentValidation;

namespace Validators;

/// <summary>
/// The rule for every text that is printed on an invoice or act PDF
/// </summary>
public static class DocumentTextRule
{
    /// <summary>
    /// The start of the message; the characters the PDF can't show follow it
    /// </summary>
    public const string Message = "Please use only characters the invoice PDF can show";

    /// <summary>
    /// Fails when the text has a character the PDF font can't draw, and names the characters.
    /// Null and empty text pass.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> DrawableOnPdf<T>(this IRuleBuilder<T, string?> ruleBuilder, IDocumentTextChecker checker)
    {
        return ruleBuilder
            .Must(text => checker.MissingCharacters(text).Count == 0)
            .WithMessage((_, text) => $"{Message} in {{PropertyName}}. Can't show: {DocumentTextFormat.Describe(checker.MissingCharacters(text))}");
    }
}
