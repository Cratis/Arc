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
    public static ConcurrencyModel? Read(INamedTypeSymbol command)
    {
        var sourceType = Dimension(command, WellKnownTypeNames.EventSourceTypeAttribute);
        var streamType = Dimension(command, WellKnownTypeNames.EventStreamTypeAttribute);
        var streamId = Dimension(command, WellKnownTypeNames.EventStreamIdAttribute);

        return sourceType is null && streamType is null && streamId is null
            ? null
            : new(false, sourceType, streamType, streamId, []);
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
