using System.Collections.Generic;

namespace ShapeCrawler.Presentations;
/// <summary>
/// Provides configuration for validation, including a set of non-critical error messages.
/// </summary>
internal static class ValidationConfig
{
    /// <summary>
    /// Retrieves a set of non-critical error messages encountered during validation. <br/>
    ///
    /// These errors indicate potential issues with the structure or attributes 
    /// of elements within the document but do not prevent the application from 
    /// functioning as intended. 
    ///
    /// This set is used to filter out validation messages that are considered 
    /// non-blocking, allowing the application to focus on critical issues only.
    /// </summary>

    public static HashSet<string> NonCriticalErrors => new HashSet<string>
    {
        "The element has unexpected child element 'http://schemas.openxmlformats.org/drawingml/2006/chart:showDLblsOverMax'.",
        "The element has invalid child element 'http://schemas.microsoft.com/office/drawing/2017/03/chart:dataDisplayOptions16'. List of possible elements expected: <http://schemas.microsoft.com/office/drawing/2017/03/chart:dispNaAsBlank>.",
        "The 'uri' attribute is not declared.",
        "The 'mod' attribute is not declared.",
        "The element has unexpected child element 'http://schemas.openxmlformats.org/drawingml/2006/main:noFill'.",
        "The element has unexpected child element 'http://schemas.openxmlformats.org/drawingml/2006/main:blipFill'."
    };
}
