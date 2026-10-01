// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.Build;

/// <summary>
/// Reports compiler and Screenplay diagnostics without changing their severity.
/// </summary>
/// <param name="log">The MSBuild task logger.</param>
public class BuildDiagnosticReporter(TaskLoggingHelper log)
{
    /// <summary>
    /// Reports a diagnostic from running the application's source generators.
    /// </summary>
    /// <param name="diagnostic">The compiler diagnostic.</param>
    public void Report(Diagnostic diagnostic)
    {
        var span = diagnostic.Location.GetMappedLineSpan();
        var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
        var line = span.StartLinePosition.Line + 1;
        var column = span.StartLinePosition.Character + 1;
        switch (diagnostic.Severity)
        {
            case DiagnosticSeverity.Error:
                log.LogError(null, diagnostic.Id, null, span.Path, line, column, 0, 0, "{0}", message);
                break;
            case DiagnosticSeverity.Warning:
                log.LogWarning(null, diagnostic.Id, null, span.Path, line, column, 0, 0, "{0}", message);
                break;
            default:
                log.LogMessage(MessageImportance.Normal, "{0}: {1}", diagnostic.Id, message);
                break;
        }
    }

    /// <summary>
    /// Reports a diagnostic from recovering, emitting, or compiling the documents.
    /// </summary>
    /// <param name="diagnostic">The Screenplay diagnostic.</param>
    public void Report(ScreenplayDiagnostic diagnostic)
    {
        var message = diagnostic.Location is null ? diagnostic.Message : $"{diagnostic.Message} ({diagnostic.Location})";
        switch (diagnostic.Severity)
        {
            case ScreenplayDiagnosticSeverity.Error:
                log.LogError(null, diagnostic.Code, null, null, 0, 0, 0, 0, "{0}", message);
                break;
            case ScreenplayDiagnosticSeverity.Warning:
                log.LogWarning(null, diagnostic.Code, null, null, 0, 0, 0, 0, "{0}", message);
                break;
            default:
                log.LogMessage(MessageImportance.Normal, "{0}: {1}", diagnostic.Code, message);
                break;
        }
    }
}
