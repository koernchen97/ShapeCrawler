using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

namespace ShapeCrawler.Presentations;
/// <summary>
/// Provides validation for OpenXml documents with filtering of non-critical errors.
/// </summary>
public class FilteredOpenXmlValidator
{
    private readonly OpenXmlValidator _validator;
    private readonly HashSet<string> _nonCriticalErrors;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilteredOpenXmlValidator"/> class.
    /// </summary>
    /// <param name="nonCriticalErrors">A set of non-critical error descriptions to be filtered out.</param>
    public FilteredOpenXmlValidator(HashSet<string> nonCriticalErrors)
    {
        _validator = new OpenXmlValidator(FileFormatVersions.Microsoft365);
        _nonCriticalErrors = nonCriticalErrors;
    }

    /// <summary>
    /// Validates the specified OpenXml document and filters out non-critical errors.
    /// </summary>
    /// <param name="document">The OpenXml document to validate.</param>
    /// <returns>An enumerable of validation errors that are not filtered out.</returns>
    public IEnumerable<ValidationErrorInfo> Validate(OpenXmlPackage document)
    {
        return _validator.Validate(document)
                         .Where(error => !_nonCriticalErrors.Contains(error.Description));
    }
}
