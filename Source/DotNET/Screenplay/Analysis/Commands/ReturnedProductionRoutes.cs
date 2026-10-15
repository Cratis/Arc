// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Recovers explicit returned destinations separately from unknown and command-context destinations.
/// </summary>
/// <param name="models">The semantic models.</param>
/// <param name="types">The shared nominal type registry.</param>
internal class ReturnedProductionRoutes(SemanticModels models, TypeRegistry types)
{
    /// <summary>
    /// Gets source definitions from the analyzed compilation set.
    /// </summary>
    internal IReadOnlyList<INamedTypeSymbol> Definitions { get; init; } = [];

    /// <summary>
    /// Attaches a proven destination and complete route without replacing unknown routing with command context.
    /// </summary>
    /// <param name="production">The recovered payload and condition.</param>
    /// <param name="creation">The event construction.</param>
    /// <param name="body">The handler body.</param>
    /// <param name="model">The semantic model.</param>
    /// <param name="command">The input owner.</param>
    /// <param name="sources">The registered generated sources.</param>
    /// <param name="generatedIdentity">Whether a generated command identifier is proven.</param>
    /// <returns>The production with independently proven routing.</returns>
    internal ProducesModel Apply(ProducesModel production, BaseObjectCreationExpressionSyntax creation, SyntaxNode body, SemanticModel model, INamedTypeSymbol command, AuthoringSources? sources, bool generatedIdentity)
    {
        var identifier = new CommandIdentifierReader(models, new ScreenplayDiagnostics()).Read(command, command.Name);
        var fallback = new CommandRouteReader(models, types, new ScreenplayDiagnostics(), true).Read(command, identifier, command.Name);
        if (creation.Ancestors().OfType<BaseObjectCreationExpressionSyntax>().FirstOrDefault(candidate =>
            model.GetTypeInfo(candidate).Type.Is("Cratis.Chronicle.EventSequences.EventForEventSourceId")) is { } wrapper)
        {
            production = production with
            {
                UsesCommandContext = production.UsesCommandContext && wrapper.Initializer is null,
                DestinationDependencies = wrapper.DescendantNodes().OfType<ExpressionSyntax>()
                    .Where(expression => !creation.Span.Contains(expression.Span))
                    .Select(expression => sources?.ReadGeneratedPath(expression, model)).OfType<string>().Distinct(StringComparer.Ordinal).ToList()
            };
            if (ExplicitEventDestination.Read(wrapper, model) is not { } envelope || envelope.Event != creation ||
                !ReturnedProductionShape.Reaches(wrapper, body, model))
            {
                return production;
            }
            var path = ProductionValues.Path(envelope.Destination, model, command, sources);
            if (path is null)
            {
                return production;
            }
            var type = DestinationType(path, envelope.Destination);
            var route = new ProductionRouteReader(models, types) { Definitions = Definitions }.Read(wrapper, command, model, fallback, type, sources);
            if (route is null && wrapper.Initializer is not null)
            {
                return production with { UsesCommandContext = false };
            }
            if (route is null && !generatedIdentity)
            {
                return production;
            }

            return production with
            {
                UsesCommandContext = route is null && path == identifier,
                Destination = new PropertyPathSource(path),
                Route = route
            };
        }
        if (generatedIdentity && production.UsesCommandContext)
        {
            // Existing generated-identity tuples are command context, not explicit per-production fan-out.
            return production;
        }
        if (creation.Ancestors().OfType<TupleExpressionSyntax>().FirstOrDefault() is { } tuple &&
            ReturnedProductionShape.Reaches(creation, body, model))
        {
            if (ReturnedProductionShape.IdentityOf(tuple, model) is { } identity)
            {
                var path = ProductionValues.Path(identity, model, command, sources);
                if (path is not null && (fallback is not null || generatedIdentity || path == identifier))
                {
                    return production with
                    {
                        UsesCommandContext = path == identifier,
                        Destination = new PropertyPathSource(path),
                        Route = path == identifier || fallback is null ? null : fallback with { IdentifierType = DestinationType(path, identity) }
                    };
                }
            }
            else if (ReturnedProductionShape.UsesCommandContext(tuple, model))
            {
                return production with { UsesCommandContext = true };
            }
        }

        return production;

        TypeReferenceModel? DestinationType(string path, ExpressionSyntax value)
        {
            var type = command.DeclaredProperties().SingleOrDefault(property => property.Name == path)?.Type ?? model.GetTypeInfo(value).Type;

            return type is null ? null : types.Resolve(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated));
        }
    }
}
