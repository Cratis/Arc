// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    /// <returns>The binding, or null when the command declares no event source.</returns>
    public static EventSourceBindingModel? Read(INamedTypeSymbol command) => Read(command, null);

    /// <summary>
    /// Reads the event source a reactor or reducer observes with <c>[FromEventSource&lt;TSource&gt;(stream)]</c>.
    /// </summary>
    /// <param name="observer">The type declaring the observer.</param>
    /// <returns>The filter, or null when the observer declares none.</returns>
    public static ObservedEventSourceModel? ReadObserved(INamedTypeSymbol observer) => ReadObserved(observer, null);

    /// <summary>
    /// Reads the concurrency scope the event source definition declares for what a command appends.
    /// </summary>
    /// <param name="command">The type declaring the command.</param>
    /// <returns>The scope, or null when no representable dimension is declared.</returns>
    public static ConcurrencyModel? ReadConcurrency(INamedTypeSymbol command) => ReadConcurrency(command, null);

    /// <summary>
    /// Reads names through attribute expressions as well as their constant values.
    /// </summary>
    /// <param name="command">The declaring command.</param>
    /// <param name="models">The semantic models resolving const references.</param>
    /// <returns>The declared binding.</returns>
    internal static EventSourceBindingModel? Read(INamedTypeSymbol command, SemanticModels? models)
    {
        var declaration = command.GetAttribute(WellKnownTypeNames.ArcEventSourceAttributeOfT);
        if (declaration?.AttributeClass is not { TypeArguments: [INamedTypeSymbol definition] })
        {
            return null;
        }
        var stream = declaration.GetArgument(0) as string;
        var declared = DeclaredStream(definition, stream);
        var name = EventSourceNames.Stream(declaration, definition, models);

        return new(SourceNameOf(definition, models), name, stream is null || (name is not null && declared is not null && EventSourceNames.Stream(declared, definition, models) == name), DimensionsOf(definition, declared).HasFlag(StreamIdDimension))
        {
            StoredSource = EventSourceNames.StoredSource(definition),
            StoredStream = stream
        };
    }

    /// <summary>
    /// Reads an observer's source and stream names from direct const references.
    /// </summary>
    /// <param name="observer">The declaring observer.</param>
    /// <param name="models">The semantic models resolving attribute syntax.</param>
    /// <returns>The observation filter.</returns>
    internal static ObservedEventSourceModel? ReadObserved(INamedTypeSymbol observer, SemanticModels? models)
    {
        var declaration = observer.GetAttribute(WellKnownTypeNames.FromEventSourceAttributeOfT);
        if (declaration?.AttributeClass is not { TypeArguments: [INamedTypeSymbol definition] })
        {
            return null;
        }
        var stream = declaration.GetArgument(0) as string;
        var name = EventSourceNames.Stream(declaration, definition, models);
        var declared = stream is not null && name is not null && definition.GetAttributes(WellKnownTypeNames.EventStreamDefinitionAttribute)
            .Any(attribute => string.Equals(attribute.GetArgument(0) as string, stream, StringComparison.Ordinal) && EventSourceNames.Stream(attribute, definition, models) == name);

        return new(SourceNameOf(definition, models), name, declared);
    }

    /// <summary>
    /// Reads concurrency dimensions using recovered model names.
    /// </summary>
    /// <param name="command">The declaring command.</param>
    /// <param name="models">The semantic models resolving stream constants.</param>
    /// <returns>The concurrency scope.</returns>
    internal static ConcurrencyModel? ReadConcurrency(INamedTypeSymbol command, SemanticModels? models)
    {
        var declaration = command.GetAttribute(WellKnownTypeNames.ArcEventSourceAttributeOfT);
        if (declaration?.AttributeClass is not { TypeArguments: [INamedTypeSymbol definition] })
        {
            return null;
        }
        var stream = declaration.GetArgument(0) as string;
        var dimensions = DimensionsOf(definition, DeclaredStream(definition, stream));
        var sourceId = dimensions.HasFlag(SourceIdDimension);
        var sourceType = dimensions.HasFlag(SourceTypeDimension) ? SourceNameOf(definition, models) : null;
        var streamType = dimensions.HasFlag(StreamTypeDimension) ? EventSourceNames.Stream(declaration, definition, models) : null;

        return sourceId || sourceType is not null || streamType is not null ? new(sourceId, sourceType, streamType, null, []) : null;
    }

    /// <summary>
    /// Reads the recoverable model source name.
    /// </summary>
    /// <param name="definition">The actual source definition.</param>
    /// <param name="models">The models recognizing const-backed definitions.</param>
    /// <returns>The model name, or the unrecoverable stored name.</returns>
    internal static string SourceNameOf(INamedTypeSymbol definition, SemanticModels? models = null) => EventSourceNames.Source(definition, models);

    /// <summary>
    /// Reads a wrapper's explicit definition and verifies exact stream ownership.
    /// </summary>
    /// <param name="definition">The selected source definition.</param>
    /// <param name="stream">The selected stream expression.</param>
    /// <param name="models">The models resolving declaration names.</param>
    /// <param name="model">The model resolving the wrapper expression.</param>
    /// <returns>The binding, or null for an undeclared or ambiguous stream.</returns>
    internal static EventSourceBindingModel? ReadDefinition(INamedTypeSymbol definition, ExpressionSyntax stream, SemanticModels models, SemanticModel model) =>
        ProductionValues.Text(stream, model) is { } stored ? ReadStoredDefinition(definition, stored, models) : null;

    /// <summary>
    /// Resolves an exact stored stream name through its owning definition's naming proof.
    /// </summary>
    /// <param name="definition">The registered definition type.</param>
    /// <param name="stored">The exact stored stream classification.</param>
    /// <param name="models">The models resolving declaration constants.</param>
    /// <returns>The binding, or null for unresolved ownership.</returns>
    internal static EventSourceBindingModel? ReadStoredDefinition(INamedTypeSymbol definition, string stored, SemanticModels models)
    {
        var declared = definition.GetAttributes(WellKnownTypeNames.EventStreamDefinitionAttribute)
            .Where(attribute => (attribute.GetArgument(0) as string) == stored).ToList();
        if (definition.FindInterface("Cratis.Chronicle.EventSources.IEventSource") is null || declared is not [var declaration] ||
            EventSourceNames.Stream(declaration, definition, models) is not { } name)
        {
            return null;
        }

        return new(SourceNameOf(definition, models), name, true, false)
        {
            StoredSource = EventSourceNames.StoredSource(definition),
            StoredStream = stored
        };
    }

    static AttributeData? DeclaredStream(INamedTypeSymbol definition, string? stream) => definition.GetAttributes(WellKnownTypeNames.EventStreamDefinitionAttribute)
        .FirstOrDefault(attribute => string.Equals(attribute.GetArgument(0) as string, stream, StringComparison.Ordinal));

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
}
