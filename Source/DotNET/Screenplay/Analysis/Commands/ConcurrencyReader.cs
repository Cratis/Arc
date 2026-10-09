// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads the scope a command's appends are checked for concurrent writers within.
/// </summary>
/// <remarks>
/// The three dimensions are declared with the same attributes that name them, and only take part in the scope when
/// they say so. An attribute that names a dimension without opting into concurrency is metadata, not a scope, and
/// is left out.
/// </remarks>
public static class ConcurrencyReader
{
    /// <summary>
    /// The name of the argument opting a dimension into the concurrency scope.
    /// </summary>
    public const string ConcurrencyArgument = "Concurrency";

    /// <summary>
    /// Reads the concurrency scope a command declares.
    /// </summary>
    /// <param name="command">The type declaring the command.</param>
    /// <returns>The <see cref="ConcurrencyModel"/>, or <see langword="null"/> when the command declares none.</returns>
    public static ConcurrencyModel? Read(INamedTypeSymbol command) => Read(command, null, null);

    /// <summary>
    /// Reads the declared scope and reports property-derived stream ids that cannot be emitted as fixed values.
    /// </summary>
    /// <param name="command">The command type.</param>
    /// <param name="diagnostics">The optional diagnostic sink.</param>
    /// <param name="location">The command's diagnostic location.</param>
    /// <returns>The representable concurrency dimensions.</returns>
    public static ConcurrencyModel? Read(INamedTypeSymbol command, ScreenplayDiagnostics? diagnostics, string? location)
    {
        var sourceType = Dimension(command, WellKnownTypeNames.EventSourceTypeAttribute);
        var streamType = Dimension(command, WellKnownTypeNames.EventStreamTypeAttribute);
        var streamId = Dimension(command, WellKnownTypeNames.EventStreamIdAttribute);
        if (streamId is not null && IsTemplate(streamId))
        {
            diagnostics?.Information(
                ScreenplayDiagnosticCodes.UnreadableCommandRoute,
                "A template concurrency stream id is property-derived; its stream id mapping was left in code",
                location ?? command.Name);
            streamId = null;
        }

        return sourceType is null && streamType is null && streamId is null
            ? null
            : new(false, sourceType, streamType, streamId, []);
    }

    /// <summary>
    /// Determines whether a stream id contains an unescaped property placeholder.
    /// </summary>
    /// <param name="value">The attribute value.</param>
    /// <returns>Whether it contains a property placeholder.</returns>
    internal static bool IsTemplate(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '{')
            {
                continue;
            }
            if (index + 1 < value.Length && value[index + 1] == '{')
            {
                index++;
                continue;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Reads one dimension of the scope.
    /// </summary>
    /// <param name="command">The type declaring the command.</param>
    /// <param name="attributeName">The fully qualified metadata name of the attribute declaring the dimension.</param>
    /// <returns>The value, or <see langword="null"/> when the dimension takes no part in the scope.</returns>
    static string? Dimension(INamedTypeSymbol command, string attributeName)
    {
        var attribute = command.GetAttribute(attributeName);
        if (attribute is null)
        {
            return null;
        }

        var concurrency = attribute.GetNamedArgument(ConcurrencyArgument) ?? attribute.GetArgument(1);

        return concurrency is true ? attribute.GetArgument(0) as string : null;
    }
}
