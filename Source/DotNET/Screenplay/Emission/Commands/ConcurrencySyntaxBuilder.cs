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
