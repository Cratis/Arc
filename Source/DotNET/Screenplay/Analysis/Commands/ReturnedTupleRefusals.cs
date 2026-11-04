// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Identifies the effects of a returned tuple whose explicit event wrapper cannot be proven to reach the returned value.
/// </summary>
/// <remarks>
/// This is a refusal-only boundary. It never admits a shape: a returned tuple carrying an explicit wrapper that
/// <see cref="ReturnedProductionShape.Reaches"/> does not accept has its known effects refused, rather than being stated
/// with the destination of the wrapper silently dropped. Only the returned value is inspected - spread operands, helper
/// bodies and nested functions are not.
/// </remarks>
/// <param name="refused">The syntax nodes of the refused effects.</param>
internal sealed class ReturnedTupleRefusals(HashSet<SyntaxNode> refused)
{
    /// <summary>
    /// Gets whether any returned effect is refused.
    /// </summary>
    internal bool Any => refused.Count > 0;

    /// <summary>
    /// Finds the refused effects of the values a handler body returns.
    /// </summary>
    /// <param name="body">The handler body.</param>
    /// <param name="model">The semantic model the body is read through.</param>
    /// <returns>The refusals.</returns>
    internal static ReturnedTupleRefusals Of(SyntaxNode body, SemanticModel model)
    {
        var refused = new HashSet<SyntaxNode>();
        foreach (var root in Returned(body))
        {
            var walk = new Walk(body, model);
            walk.Collect(root);
            if (Unwrap(root) is TupleExpressionSyntax && walk.Wrappers.Exists(wrapper => !ReturnedProductionShape.Reaches(wrapper.Wrapper, body, model)))
            {
                refused.UnionWith(walk.Effects);
            }
        }

        return new(refused);
    }

    /// <summary>
    /// Finds the refusals of an ordinary handler body, which never apply to a handler taking generated values.
    /// </summary>
    /// <param name="handler">The handler method.</param>
    /// <param name="body">The handler body.</param>
    /// <param name="model">The semantic model the body is read through.</param>
    /// <param name="enabled">Whether routes are being recovered at all.</param>
    /// <returns>The refusals, none when not applicable.</returns>
    internal static ReturnedTupleRefusals For(IMethodSymbol handler, SyntaxNode body, SemanticModel model, bool enabled) =>
        enabled && !handler.Parameters.Any(parameter => parameter.HasAttribute(WellKnownTypeNames.GeneratedValueAttribute)) ? Of(body, model) : new([]);

    /// <summary>
    /// Determines whether an event construction is part of a refused returned value.
    /// </summary>
    /// <param name="creation">The event construction.</param>
    /// <returns>True when its production must be refused.</returns>
    internal bool Covers(SyntaxNode creation) => refused.Count > 0 && creation.AncestorsAndSelf().Any(refused.Contains);

    static IEnumerable<ExpressionSyntax> Returned(SyntaxNode body) => body switch
    {
        ExpressionSyntax expression => [expression],
        _ => body.DescendantNodes(node => node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
            .OfType<ReturnStatementSyntax>().Select(statement => statement.Expression).OfType<ExpressionSyntax>()
    };

    static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }

    sealed class Walk(SyntaxNode body, SemanticModel model)
    {
        readonly HashSet<ISymbol> _visited = new(SymbolEqualityComparer.Default);

        internal List<BaseObjectCreationExpressionSyntax> Effects { get; } = [];

        internal List<ExplicitEventDestination> Wrappers { get; } = [];

        internal void Collect(ExpressionSyntax expression)
        {
            var value = Unwrap(expression);
            if (ElementsOf(value) is { } elements)
            {
                CollectAll(elements);
            }
            else if (value is BaseObjectCreationExpressionSyntax creation)
            {
                CollectCreation(creation);
            }
            else if (value is IdentifierNameSyntax identifier)
            {
                CollectLocal(identifier);
            }
        }

        static IEnumerable<ExpressionSyntax>? ElementsOf(ExpressionSyntax value) => value switch
        {
            TupleExpressionSyntax tuple => tuple.Arguments.Select(argument => argument.Expression),
            CollectionExpressionSyntax collection => collection.Elements.OfType<ExpressionElementSyntax>().Select(element => element.Expression),
            ArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions,
            ImplicitArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions,
            _ => null
        };

        void CollectLocal(IdentifierNameSyntax identifier)
        {
            if (model.GetSymbolInfo(identifier).Symbol is ILocalSymbol local && _visited.Add(local) &&
                local.DeclaringSyntaxReferences is [{ } reference] &&
                reference.GetSyntax() is VariableDeclaratorSyntax { Initializer.Value: { } value } && body.Span.Contains(value.Span))
            {
                Collect(value);
            }
        }

        void CollectAll(IEnumerable<ExpressionSyntax> expressions)
        {
            foreach (var expression in expressions)
            {
                Collect(expression);
            }
        }

        void CollectCreation(BaseObjectCreationExpressionSyntax creation)
        {
            if (ExplicitEventDestination.Read(creation, model) is { } envelope)
            {
                Wrappers.Add(envelope);
                Effects.Add(creation);
            }
            else if (model.GetTypeInfo(creation).Type is INamedTypeSymbol type && EventReader.IsEvent(type))
            {
                Effects.Add(creation);
            }
        }
    }
}
