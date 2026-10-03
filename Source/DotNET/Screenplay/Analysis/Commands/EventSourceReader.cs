// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads the event source definition a command declares with <c>[EventSource&lt;TSource&gt;(stream)]</c>.
/// </summary>
public static class EventSourceReader
{
    /// <summary>
    /// The name of the argument of the definition attributes carrying the concurrency dimensions.
    /// </summary>
    public const string ConcurrencyArgument = "Concurrency";

    const int SourceIdDimension = 1;
    const int SourceTypeDimension = 2;
    const int StreamTypeDimension = 4;
    const int StreamIdDimension = 8;

    /// <summary>
    /// Reads the event source a command declares.
    /// </summary>
    /// <param name="command">The type declaring the command.</param>
    /// <returns>The <see cref="EventSourceBindingModel"/>, or <see langword="null"/> when the command declares no event source.</returns>
    public static EventSourceBindingModel? Read(INamedTypeSymbol command)
    {
        var declaration = command.GetAttribute(WellKnownTypeNames.ArcEventSourceAttributeOfT);
        if (declaration?.AttributeClass is not { TypeArguments: [INamedTypeSymbol definition] })
        {
            return null;
        }

        var stream = declaration.GetArgument(0) as string;
        var declared = definition.GetAttributes(WellKnownTypeNames.EventStreamDefinitionAttribute)
            .FirstOrDefault(_ => string.Equals(_.GetArgument(0) as string, stream, StringComparison.Ordinal));

        return new(SourceNameOf(definition), stream, stream is null || declared is not null, DimensionsOf(definition, declared).HasFlag(StreamIdDimension));
    }

    /// <summary>
    /// Reads the concurrency scope the event source definition declares for what a command appends.
    /// </summary>
    /// <param name="command">The type declaring the command.</param>
    /// <returns>The <see cref="ConcurrencyModel"/>, or <see langword="null"/> when the command declares no event source or the definition narrows nothing Screenplay can state.</returns>
    /// <remarks>
    /// The scope is the stream's own dimensions when it declares any, otherwise the event source's. The stream id
    /// dimension has no value at declaration time, since it is only known when a command appends, so it cannot be
    /// stated and is reported through <see cref="EventSourceBindingModel.ConcurrentByStreamId"/>.
    /// </remarks>
    public static ConcurrencyModel? ReadConcurrency(INamedTypeSymbol command)
    {
        var declaration = command.GetAttribute(WellKnownTypeNames.ArcEventSourceAttributeOfT);
        if (declaration?.AttributeClass is not { TypeArguments: [INamedTypeSymbol definition] })
        {
            return null;
        }

        var stream = declaration.GetArgument(0) as string;
        var declared = definition.GetAttributes(WellKnownTypeNames.EventStreamDefinitionAttribute)
            .FirstOrDefault(_ => string.Equals(_.GetArgument(0) as string, stream, StringComparison.Ordinal));
        var dimensions = DimensionsOf(definition, declared);

        var sourceId = dimensions.HasFlag(SourceIdDimension);
        var sourceType = dimensions.HasFlag(SourceTypeDimension) ? SourceNameOf(definition) : null;
        var streamType = dimensions.HasFlag(StreamTypeDimension) ? stream : null;

        return sourceId || sourceType is not null || streamType is not null
            ? new(sourceId, sourceType, streamType, null, [])
            : null;
    }

    static int DimensionsOf(INamedTypeSymbol definition, AttributeData? stream)
    {
        var streamDimensions = stream?.GetNamedArgument(ConcurrencyArgument) is int own ? own : 0;
        if (streamDimensions != 0)
        {
            return streamDimensions;
        }

        return definition.GetAttribute(WellKnownTypeNames.EventSourceDefinitionAttribute)?.GetNamedArgument(ConcurrencyArgument) is int inherited ? inherited : 0;
    }

    static bool HasFlag(this int dimensions, int flag) => (dimensions & flag) == flag;

    static string SourceNameOf(INamedTypeSymbol definition)
    {
        if (definition.GetAttribute(WellKnownTypeNames.EventSourceDefinitionAttribute)?.GetArgument(0) is string name)
        {
            return name;
        }

        const string suffix = "EventSource";

        return definition.Name.Length > suffix.Length && definition.Name.EndsWith(suffix, StringComparison.Ordinal)
            ? definition.Name[..^suffix.Length]
            : definition.Name;
    }
}
