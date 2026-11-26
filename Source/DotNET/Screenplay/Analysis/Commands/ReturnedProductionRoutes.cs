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
        var context = new RouteContext(model, command, sources, generatedIdentity, identifier, fallback, creation, body);
        if (creation.Ancestors().OfType<BaseObjectCreationExpressionSyntax>().FirstOrDefault(candidate =>
            model.GetTypeInfo(candidate).Type.Is(EventWrapperType.MetadataName)) is { } wrapper)
        {
            return ApplyWrapper(production, wrapper, context);
        }
        if (generatedIdentity && production.UsesCommandContext)
        {
            // Existing generated-identity tuples are command context, not explicit per-production fan-out.
            return (production, false);
        }

        return (ApplyTuple(production, context), false);
    }

    static bool HasUnknownDestination(ProducesModel production) => !production.UsesCommandContext && production.Destination is null &&
        production.UnavailableCondition is null && production.UnavailableDestination is null;

    static bool SameEffectiveRoute(CommandRouteModel route, CommandRouteModel fallback) => route.Identity == fallback.Identity &&
        route.StreamIdType == fallback.StreamIdType && route.StreamId == fallback.StreamId && route.StreamIdLiteral == fallback.StreamIdLiteral &&
        route.StreamIdParts.SequenceEqual(fallback.StreamIdParts);

    static bool IsReturnedTuple(SyntaxNode body, BaseObjectCreationExpressionSyntax wrapper) =>
        body is TupleExpressionSyntax || wrapper.Ancestors().TakeWhile(ancestor => ancestor != body).Any(ancestor => ancestor is TupleExpressionSyntax);

    static bool IsProvenCommandDestination(string path, BaseObjectCreationExpressionSyntax wrapper, RouteContext context) =>
        context.GeneratedIdentity || (path == context.Identifier && ReturnedProductionShape.WithinOneOf(wrapper, context.Model));

    static bool CanRouteIdentity(string? path, RouteContext context) =>
        path is not null && (context.Fallback is not null || context.GeneratedIdentity || path == context.Identifier);

    static ProducesModel WithDestinationDependencies(ProducesModel production, BaseObjectCreationExpressionSyntax wrapper, RouteContext context) =>
        production with
        {
            UsesCommandContext = false,
            DestinationDependencies = wrapper.DescendantNodes().OfType<ExpressionSyntax>()
                .Where(expression => !context.Creation.Span.Contains(expression.Span))
                .Select(expression => context.Sources?.ReadGeneratedPath(expression, context.Model)).OfType<string>().Distinct(StringComparer.Ordinal).ToList()
        };

    static ProducesModel RecoverRouted(ProducesModel candidate, string path, CommandRouteModel route, bool usesCommandContext, RouteContext context) =>
        usesCommandContext && context.Fallback is not null && SameEffectiveRoute(route, context.Fallback)
            ? candidate with { UsesCommandContext = true }
            : candidate with { UsesCommandContext = false, Destination = new PropertyPathSource(path), Route = route };

    static ProducesModel RecoverUnrouted(ProducesModel candidate, string path, BaseObjectCreationExpressionSyntax wrapper, RouteContext context)
    {
        if (wrapper.Initializer is not null)
        {
            return candidate with { UsesCommandContext = false };
        }

        return IsProvenCommandDestination(path, wrapper, context)
            ? candidate with { UsesCommandContext = path == context.Identifier, Destination = new PropertyPathSource(path), Route = null }
            : candidate;
    }

    (ProducesModel Production, bool UnsafeReturnedTupleWrapper) ApplyWrapper(ProducesModel production, BaseObjectCreationExpressionSyntax wrapper, RouteContext context)
    {
        var usesCommandContext = production.UsesCommandContext;
        production = WithDestinationDependencies(production, wrapper, context);
        if (ExplicitEventDestination.Read(wrapper, context.Model) is not { } envelope || envelope.Event != context.Creation ||
            !ReturnedProductionShape.Reaches(wrapper, context.Body, context.Model))
        {
            return (production, false);
        }
        var recovered = RecoverDestination(production, envelope, usesCommandContext, context);

        return (recovered, IsReturnedTuple(context.Body, wrapper) && HasUnknownDestination(recovered));
    }

    ProducesModel ApplyTuple(ProducesModel production, RouteContext context)
    {
        if (context.Creation.Ancestors().OfType<TupleExpressionSyntax>().FirstOrDefault() is not { } tuple ||
            !ReturnedProductionShape.Reaches(context.Creation, context.Body, context.Model))
        {
            return production;
        }
        if (ReturnedProductionShape.IdentityOf(tuple, context.Model) is { } identity)
        {
            return ApplyTupleIdentity(production, identity, context);
        }

        return ReturnedProductionShape.UsesCommandContext(tuple, context.Model) ? production with { UsesCommandContext = true } : production;
    }

    ProducesModel ApplyTupleIdentity(ProducesModel production, ExpressionSyntax identity, RouteContext context)
    {
        var path = ProductionValues.Path(identity, context.Model, context.Command, context.Sources);
        if (path is null || !CanRouteIdentity(path, context))
        {
            return production;
        }
        var isCommandIdentity = path == context.Identifier;

        return production with
        {
            UsesCommandContext = isCommandIdentity,
            Destination = new PropertyPathSource(path),
            Route = isCommandIdentity || context.Fallback is null ? null : context.Fallback with { IdentifierType = DestinationType(path, identity, context) }
        };
    }

    ProducesModel RecoverDestination(ProducesModel candidate, ExplicitEventDestination envelope, bool usesCommandContext, RouteContext context)
    {
        var wrapper = envelope.Wrapper;
        if (usesCommandContext && wrapper.Initializer is null)
        {
            return candidate with { UsesCommandContext = true };
        }
        if (ProductionValues.Path(envelope.Destination, context.Model, context.Command, context.Sources) is not { } path)
        {
            return candidate;
        }
        var type = DestinationType(path, envelope.Destination, context);
        var route = new ProductionRouteReader(models, types) { Definitions = Definitions }.Read(wrapper, context.Command, context.Model, context.Fallback, type, context.Sources);

        return route is null
            ? RecoverUnrouted(candidate, path, wrapper, context)
            : RecoverRouted(candidate, path, route, usesCommandContext, context);
    }

    TypeReferenceModel? DestinationType(string path, ExpressionSyntax value, RouteContext context)
    {
        var type = context.Command.DeclaredProperties().SingleOrDefault(property => property.Name == path)?.Type ?? context.Model.GetTypeInfo(value).Type;

        return type is null ? null : types.Resolve(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated));
    }

    sealed record RouteContext(SemanticModel Model, INamedTypeSymbol Command, AuthoringSources? Sources, bool GeneratedIdentity, string? Identifier, CommandRouteModel? Fallback, BaseObjectCreationExpressionSyntax Creation, SyntaxNode Body);
}
