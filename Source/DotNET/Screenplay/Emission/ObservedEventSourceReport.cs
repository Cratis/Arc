// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// Reports what of the event source a reactor or reducer is filtered to the language cannot state.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// The latest published Screenplay syntax has no way to narrow a reaction trigger or a projection to an event source
/// or stream, and the model carries no such construct to invent one for. The filter is therefore never written, and
/// reporting it is what keeps the document from reading as an observer of every event of its types.
/// </remarks>
public class ObservedEventSourceReport(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Reports the event source an observer is filtered to.
    /// </summary>
    /// <param name="kind">What the observer is, for the message.</param>
    /// <param name="name">The name of the observer.</param>
    /// <param name="eventSource">The event source the observer is filtered to, if it declares one.</param>
    /// <param name="location">Where the observer lives, for use in diagnostics.</param>
    public void Report(string kind, string name, ObservedEventSourceModel? eventSource, string location)
    {
        if (eventSource is null)
        {
            return;
        }

        var stream = eventSource.Stream is null ? string.Empty : $" stream '{eventSource.Stream}'";
        if (!eventSource.StreamDeclared)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.ObserverEventStreamNotDeclared,
                $"The {kind} '{name}' observes{stream} of event source '{eventSource.Source}', which the event source does not declare",
                location);
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.ObserverEventSourceNotRepresentable,
            $"The {kind} '{name}' only observes events of event source '{eventSource.Source}'{stream}, which the language cannot declare yet; it is emitted observing its events from every event source",
            location);
    }
}
