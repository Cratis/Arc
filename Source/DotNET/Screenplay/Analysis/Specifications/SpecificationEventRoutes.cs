// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads only concrete routing supplied to the event sequence, never routing guessed from event attributes.
/// </summary>
public static class SpecificationEventRoutes
{
    /// <summary>
    /// Reads a direct append or a builder known to append without routing metadata.
    /// </summary>
    /// <param name="invocation">The call appending an occurrence.</param>
    /// <param name="method">The resolved append method.</param>
    /// <param name="model">The semantic model resolving arguments.</param>
    /// <param name="draft">The scenario collecting evidence.</param>
    /// <returns>The proven route, or null when the call states no route this reader can prove.</returns>
    public static SpecificationEventRouteModel? Read(InvocationExpressionSyntax invocation, IMethodSymbol method, SemanticModel model, SpecificationDraft draft)
    {
        if (method.ContainingType.Is(WellKnownTypeNames.EventSourceGivenBuilder) || method.ContainingType.Is(WellKnownTypeNames.EventSourceWhenBuilder))
        {
            return SpecificationEventRouteModel.NoStream;
        }

        if (!method.ContainingType.Is(WellKnownTypeNames.EventSequence) && method.ContainingType.FindInterface(WellKnownTypeNames.EventSequence) is null)
        {
            return null;
        }

        var source = Argument("eventSourceType") ?? Argument("eventSource");
        var stream = Argument("eventStreamType") ?? Argument("eventStream");
        var id = Argument("eventStreamId");
        if (source is null && stream is null && id is null)
        {
            return SpecificationEventRouteModel.NoStream;
        }

        var sourceName = Text(source);
        var streamName = Text(stream);
        var streamId = id is null ? null : Text(id);
        var naming = new ScreenplayNaming();
        if (sourceName is null || streamName is null || !ScreenplayIdentifier.IsBareIdentifier(sourceName) || !ScreenplayIdentifier.IsBareIdentifier(streamName) ||
            (id is not null && (streamId is null || streamId.Length == 0 || !streamId.IsNormalized() || naming.ToStringLiteral(streamId) != streamId)))
        {
            draft.CannotRead("its event route is not a concrete source-owned stream with a portable stream id; no route was inferred");
            return null;
        }

        return new(sourceName, streamName, streamId is null ? null : new(streamId));

        ExpressionSyntax? Argument(string parameter) => CallArguments.For(invocation, method, parameter).SingleOrDefault();

        string? Text(ExpressionSyntax? expression)
        {
            if (expression is null)
            {
                return null;
            }

            expression = MappingSourceReader.Unwrap(expression);
            if (expression is BaseObjectCreationExpressionSyntax { ArgumentList.Arguments: [var argument], Initializer: null } &&
                model.GetTypeInfo(expression).Type?.FindBase(WellKnownTypeNames.ConceptAs) is not null)
            {
                expression = argument.Expression;
            }

            return model.GetConstantValue(expression) is { HasValue: true, Value: string value } ? value : null;
        }
    }
}
