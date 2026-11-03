using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.Api;

/// <summary>Brazilian document syntax, check digits and formatting.</summary>
[ApiController]
[Route("api/v1/br/documents")]
[Tags("Brazilian documents")]
[ProducesResponseType<ProblemDetails>(429)]
[ProducesResponseType<ProblemDetails>(500)]
[ProducesResponseType<ValidationProblemDetails>(400)]
public sealed class DocumentsController : ControllerBase
{
    /// <summary>Validate CPF syntax and check digits.</summary>
    [HttpPost("cpf/validate")]
    public ActionResult<DocumentValidation> Cpf(DocumentRequest request) => Validate("cpf", request);

    /// <summary>Validate numeric or alphanumeric CNPJ syntax and check digits.</summary>
    [HttpPost("cnpj/validate")]
    public ActionResult<DocumentValidation> Cnpj(DocumentRequest request) => Validate("cnpj", request);

    /// <summary>Validate PIS/PASEP syntax and check digit.</summary>
    [HttpPost("pis/validate")]
    public ActionResult<DocumentValidation> Pis(DocumentRequest request) => Validate("pis", request);

    /// <summary>Apply the standard CPF or CNPJ mask while preserving leading zeroes.</summary>
    [HttpPost("format")]
    public ActionResult<FormattedDocument> Format(FormatDocumentRequest request)
    {
        var formatted = Documents.Format(request.Type, request.Value);
        if (formatted is not null)
            return new FormattedDocument(request.Type, formatted);

        ModelState.AddModelError("value", "Use a CPF or CNPJ matching the selected type.");
        return ValidationProblem(ModelState);
    }

    private ActionResult<DocumentValidation> Validate(string type, DocumentRequest request)
    {
        var normalized = Documents.Normalize(type, request.Value);
        if (normalized is null)
        {
            ModelState.AddModelError("value", "The document has an invalid format.");
            return ValidationProblem(ModelState);
        }

        var valid = Documents.IsValid(type, normalized);
        return new DocumentValidation(valid, valid ? null : "Invalid check digits or repeated digits.");
    }
}

/// <summary>A document with or without its standard punctuation.</summary>
public record DocumentRequest([Required, StringLength(18)] string Value);

/// <summary>A CPF or CNPJ to format.</summary>
public sealed record FormatDocumentRequest(
    [Required, RegularExpression("cpf|cnpj"), DefaultValue("cnpj")] string Type,
    [Required, StringLength(18), DefaultValue("12ABC34501DE35")] string Value);

/// <summary>The check digit result.</summary>
public sealed record DocumentValidation(bool IsValid, string? Reason);

/// <summary>The document in its standard display format.</summary>
public sealed record FormattedDocument(string Type, string Formatted);

