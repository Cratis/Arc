// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Filters only the informational timeline findings introduced by Screenplay 4.69.
/// </summary>
static class TimelineDiagnostics
{
    /// <summary>
    /// Keeps warnings, errors and information outside the known timeline findings.
    /// </summary>
    /// <param name="diagnostics">The compiler diagnostics to filter.</param>
    /// <returns>All diagnostics except informational PLAY0516 and PLAY0517 findings.</returns>
    public static IEnumerable<Diagnostic> WithoutTimelineInformation(this IEnumerable<Diagnostic> diagnostics) =>
        diagnostics.Where(_ => _.Severity != DiagnosticSeverity.Information || _.Code is not ("PLAY0516" or "PLAY0517"));
}
