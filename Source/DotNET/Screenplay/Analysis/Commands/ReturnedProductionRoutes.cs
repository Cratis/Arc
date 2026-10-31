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
    /// <returns>The production with independently proven routing and unsafe returned-tuple wrapper provenance.</returns>
    internal (ProducesModel Production, bool UnsafeReturnedTupleWrapper) Apply(ProducesModel production, BaseObjectCreationExpressionSyntax creation, SyntaxNode body, SemanticModel model, INamedTypeSymbol command, AuthoringSources? sources, bool generatedIdentity)
    {
        var identifier = new CommandIdentifierReader(models, new ScreenplayDiagnostics()).Read(command, command.Name);
        var fallback = new CommandRouteReader(models, types, new ScreenplayDiagnostics(), true).Read(command, identifier, command.Name);
        if (creation.Ancestors().OfType<BaseObjectCreationExpressionSyntax>().FirstOrDefault(candidate =>
            model.GetTypeInfo(candidate).Type.Is(EventWrapperType.MetadataName)) is { } wrapper)
        {
            var usesCommandContext = production.UsesCommandContext;
            production = production with
            {
                UsesCommandContext = false,
                DestinationDependencies = wrapper.DescendantNodes().OfType<ExpressionSyntax>()
                    .Where(expression => !creation.Span.Contains(expression.Span))
                    .Select(expression => sources?.ReadGeneratedPath(expression, model)).OfType<string>().Distinct(StringComparer.Ordinal).ToList()
            };
            if (ExplicitEventDestination.Read(wrapper, model) is not { } envelope || envelope.Event != creation ||
                !ReturnedProductionShape.Reaches(wrapper, body, model))
            {
                return (production, false);
            }
            var recovered = RecoverDestination(production, envelope, usesCommandContext);
            var returnedTuple = body is TupleExpressionSyntax || wrapper.Ancestors().TakeWhile(ancestor => ancestor != body).Any(ancestor => ancestor is TupleExpressionSyntax);

            return (recovered, returnedTuple && HasUnknownDestination(recovered));
        }
        if (generatedIdentity && production.UsesCommandContext)
        {
            // Existing generated-identity tuples are command context, not explicit per-production fan-out.
            return (production, false);
        }
        if (creation.Ancestors().OfType<TupleExpressionSyntax>().FirstOrDefault() is { } tuple &&
            ReturnedProductionShape.Reaches(creation, body, model))
        {
            if (ReturnedProductionShape.IdentityOf(tuple, model) is { } identity)
            {
                var path = ProductionValues.Path(identity, model, command, sources);
                if (path is not null && (fallback is not null || generatedIdentity || path == identifier))
                {
                    return (production with
                    {
                        UsesCommandContext = path == identifier,
                        Destination = new PropertyPathSource(path),
                        Route = path == identifier || fallback is null ? null : fallback with { IdentifierType = DestinationType(path, identity) }
                    }, false);
                }
            }
            else if (ReturnedProductionShape.UsesCommandContext(tuple, model))
            {
                return (production with { UsesCommandContext = true }, false);
            }
        }

        return (production, false);

        ProducesModel RecoverDestination(ProducesModel candidate, ExplicitEventDestination envelope, bool usesCommandContext)
        {
            var destinationWrapper = envelope.Wrapper;
            if (usesCommandContext && destinationWrapper.Initializer is null)
            {
                return candidate with { UsesCommandContext = true };
            }
            var path = ProductionValues.Path(envelope.Destination, model, command, sources);
            if (path is null)
            {
                return candidate;
            }
            var type = DestinationType(path, envelope.Destination);
            var route = new ProductionRouteReader(models, types) { Definitions = Definitions }.Read(destinationWrapper, command, model, fallback, type, sources);
            if (route is null && destinationWrapper.Initializer is not null)
            {
                return candidate with { UsesCommandContext = false };
            }
            if (route is null && !generatedIdentity)
            {
                return candidate;
            }
            if (usesCommandContext && route is not null && fallback is not null && SameEffectiveRoute(route, fallback))
            {
                return candidate with { UsesCommandContext = true };
            }

            return candidate with
            {
                UsesCommandContext = route is null && path == identifier,
                Destination = new PropertyPathSource(path),
                Route = route
            };
        }

        TypeReferenceModel? DestinationType(string path, ExpressionSyntax value)
        {
            var type = command.DeclaredProperties().SingleOrDefault(property => property.Name == path)?.Type ?? model.GetTypeInfo(value).Type;

            return type is null ? null : types.Resolve(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated));
        }
    }

    static bool HasUnknownDestination(ProducesModel production) => !production.UsesCommandContext && production.Destination is null &&
        production.UnavailableCondition is null && production.UnavailableDestination is null;

    static bool SameEffectiveRoute(CommandRouteModel route, CommandRouteModel fallback) => route.Identity == fallback.Identity &&
        route.StreamIdType == fallback.StreamIdType && route.StreamId == fallback.StreamId && route.StreamIdLiteral == fallback.StreamIdLiteral &&
        route.StreamIdParts.SequenceEqual(fallback.StreamIdParts);
}
