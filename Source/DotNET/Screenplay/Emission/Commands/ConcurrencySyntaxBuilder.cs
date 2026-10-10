// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Commands;

/// <summary>
/// Builds the Screenplay <c>concurrency</c> block for a command.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// A concurrency block that narrows nothing at all does not compile, so a scope carrying no dimension is reported
/// and left out rather than emitted empty.
/// </remarks>
public class ConcurrencySyntaxBuilder(IScreenplayNaming naming, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Gets whether executable event routing is admitted by the requested cap.
    /// </summary>
    public bool ExecutableRoutes { get; init; }

    /// <summary>
    /// Reports a concurrency-only command whose legacy scope has no executable route counterpart.
    /// </summary>
    /// <param name="location">The command's diagnostic location.</param>
    public void ReportLegacyScope(string location) => diagnostics.Information(ScreenplayDiagnosticCodes.EventSourceNotRepresentable, "The command has only legacy concurrency metadata, with no admitted event route counterpart; its concurrency block retains its legacy meaning (PLAY0271)", location);

    /// <summary>
    /// Reports the concurrency dimensions omitted when a command states an executable route.
    /// </summary>
    /// <param name="concurrency">The omitted scope.</param>
    /// <param name="location">The command's diagnostic location.</param>
    public void ReportOmittedScope(ConcurrencyModel concurrency, string location)
    {
        var dimensions = new List<string>();
        if (concurrency.EventSource)
        {
            dimensions.Add("event source id");
        }
        if (concurrency.SourceType is { } source)
        {
            dimensions.Add($"source type '{source}'");
        }
        if (concurrency.StreamType is { } stream)
        {
            dimensions.Add($"stream type '{stream}'");
        }
        if (concurrency.StreamId is { } streamId)
        {
            dimensions.Add($"stream id '{streamId}'");
        }
        dimensions.AddRange(concurrency.EventTypes.Select(eventType => $"event type '{eventType}'"));
        diagnostics.Information(ScreenplayDiagnosticCodes.EventSourceNotRepresentable,
            $"The command's route is stated, but concurrency dimensions [{string.Join(", ", dimensions)}] were left out; ESM v8 has no executable concurrency syntax (PLAY0271)", location);
    }

    /// <summary>
    /// Builds the concurrency block a command declares.
    /// </summary>
    /// <param name="concurrency">The scope to build for, if the command declares one.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The <see cref="ConcurrencySyntax"/>, or <see langword="null"/> when there is nothing to declare.</returns>
    public ConcurrencySyntax? Build(ConcurrencyModel? concurrency, string location)
    {
        if (concurrency is null)
        {
            return null;
        }

        var sourceType = ToIdentifier(concurrency.SourceType);
        var streamType = ToIdentifier(concurrency.StreamType);
        var streamId = ToIdentifier(concurrency.StreamId);
        var eventTypes = concurrency.EventTypes
            .Select(naming.ToDeclarationName)
            .Where(_ => _.Length > 1)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (!concurrency.EventSource && sourceType is null && streamType is null && streamId is null && eventTypes.Count == 0)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.EmptyConcurrencyScope,
                "The concurrency scope narrows nothing at all and was left out",
                location);

            return null;
        }

        return new(concurrency.EventSource, sourceType, streamType, streamId, eventTypes, SourceLocation.Start);
    }

    /// <summary>
    /// Reports what of the event source a command appends through the language cannot state.
    /// </summary>
    /// <param name="eventSource">The event source the command appends through, if it declares one.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="authoringOnlyConstructs">Whether authoring-only constructs are enabled.</param>
    public void ReportEventSource(EventSourceBindingModel? eventSource, string location, bool authoringOnlyConstructs = false) =>
        ReportEventSource(eventSource, location, authoringOnlyConstructs, null);

    /// <summary>Reports unrepresented event source routing for a named command.</summary>
    /// <param name="eventSource">The event source the command appends through, if it declares one.</param>
    /// <param name="location">The command's diagnostic location.</param>
    /// <param name="authoringOnlyConstructs">Whether authoring-only constructs are enabled.</param>
    /// <param name="commandName">The command appending through the event source.</param>
    public void ReportEventSource(EventSourceBindingModel? eventSource, string location, bool authoringOnlyConstructs, string? commandName)
    {
        if (eventSource is null)
        {
            return;
        }

        var stream = eventSource.Stream is null ? string.Empty : $" stream '{eventSource.Stream}'";
        if (!eventSource.StreamDeclared)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.EventStreamNotDeclared,
                $"The command names{stream} of event source '{eventSource.Source}', which the event source does not declare",
                location);
        }

        var command = commandName is null ? "The command" : $"The command '{commandName}'";
        var message = authoringOnlyConstructs || ExecutableRoutes
            ? $"{command} appends through event source '{eventSource.Source}'{stream}, but no unambiguous readable route could be stated; only the existing concurrency dimensions are emitted"
            : $"{command} appends through event source '{eventSource.Source}'{stream}; source and stream declarations are authoring-only and can be enabled with ScreenplayOptions.AuthoringOnlyConstructs; only the existing concurrency dimensions are emitted";
        if (authoringOnlyConstructs)
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.EventSourceNotRepresentable, message, location);
        }
        else
        {
            diagnostics.Information(ScreenplayDiagnosticCodes.EventSourceNotRepresentable, message, location);
        }

        if (eventSource.ConcurrentByStreamId)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.EventStreamIdConcurrencyNotRepresentable,
                $"The stream id takes part in the concurrency scope of event source '{eventSource.Source}', which has no value to state and was left out",
                location);
        }
    }

    /// <summary>Reports legacy classification attributes omitted from executable-default output.</summary>
    /// <param name="location">The diagnostic location.</param>
    public void ReportLegacyRoute(string location) => ReportLegacyRoute(location, null);

    /// <summary>Reports legacy classification attributes omitted for a named command.</summary>
    /// <param name="location">The diagnostic location.</param>
    /// <param name="commandName">The routed command, when it is known.</param>
    public void ReportLegacyRoute(string location, string? commandName) => diagnostics.Information(ScreenplayDiagnosticCodes.EventSourceNotRepresentable, ExecutableRoutes ? $"Event source and stream routing{(commandName is null ? string.Empty : $" for command '{commandName}'")} has no unambiguous admitted route; only the existing concurrency dimensions are emitted" : $"Event source and stream routing{(commandName is null ? string.Empty : $" for command '{commandName}'")} are authoring-only; enable ScreenplayOptions.AuthoringOnlyConstructs to describe readable routes", location);

    /// <summary>Reports a dynamic concurrency flag that the current grammar cannot state without a value.</summary>
    /// <param name="location">The diagnostic location.</param>
    public void ReportStreamIdFlag(string location) => diagnostics.Warning(
        ScreenplayDiagnosticCodes.EventStreamIdConcurrencyNotRepresentable,
        "Dynamic stream-id concurrency has no valueless flag in the current grammar and was left out",
        location);

    /// <summary>
    /// Converts a dimension of the scope into the identifier it is written as.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The identifier, or <see langword="null"/> when nothing should be emitted.</returns>
    string? ToIdentifier(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var identifier = naming.ToDeclarationName(value);

        return identifier.Length <= 1 ? null : identifier;
    }
}
