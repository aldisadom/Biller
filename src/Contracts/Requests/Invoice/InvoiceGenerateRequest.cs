using Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace Contracts.Requests.Invoice;

public record InvoiceGenerateRequest
{
    [Required]
    public Guid Id { get; set; }
    public Language LanguageCode { get; set; } = Language.LT;
    public DocumentType DocumentType { get; set; } = DocumentType.JobDoneAct;
}
