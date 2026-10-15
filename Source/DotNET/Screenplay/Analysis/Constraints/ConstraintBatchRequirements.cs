// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Constraints;

/// <summary>
/// Records every uniqueness declaration a returned batch needs, including declarations the reader cannot emit.
/// </summary>
internal sealed class ConstraintBatchRequirements
{
    readonly List<(IReadOnlySet<string> Targets, ConstraintModel? Rule, string Name)> _rules = [];

    /// <summary>
    /// Reads one complete uniqueness declaration with the same reader used for constraint emission.
    /// </summary>
    /// <param name="constraint">The declaring constraint type.</param>
    /// <param name="invocation">The uniqueness call.</param>
    /// <param name="method">The resolved uniqueness method.</param>
    /// <param name="model">The source semantic model.</param>
    internal void Read(INamedTypeSymbol constraint, InvocationExpressionSyntax invocation, IMethodSymbol method, SemanticModel model)
    {
        var targets = method.TypeArguments.Concat(invocation.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(call => model.GetSymbolInfo(call).Symbol).OfType<IMethodSymbol>()
            .Where(call => call.Name == "On" && call.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Events.Constraints")
            .SelectMany(call => call.TypeArguments)).Select(EventProducers.IdentityOf).ToHashSet(StringComparer.Ordinal);
        _rules.Add((targets, ConstraintReader.ReadUnique(constraint, invocation, model), constraint.Name));
    }

    /// <summary>
    /// Collects all rejecting constraints, not just the first constraint on a later event.
    /// </summary>
    /// <param name="events">The returned batch's event types.</param>
    /// <param name="rejection">The legacy per-fact equivalence gap, if any.</param>
    /// <returns>The required declarations or an unrepresentable-constraint gap.</returns>
    internal ReactionBatchGap? For(IReadOnlyList<INamedTypeSymbol> events, string? rejection)
    {
        var classUnique = events.FirstOrDefault(type => type.HasAttribute(WellKnownTypeNames.UniqueAttribute));
        if (classUnique is not null)
        {
            return new(ReactionBatchGapKind.Unrepresented, $"the class-level uniqueness constraint on '{classUnique.Name}' is not represented in the document")
            {
                RequiresAtomic = rejection is not null
            };
        }

        var identities = events.Select(EventProducers.IdentityOf).ToHashSet(StringComparer.Ordinal);
        var required = _rules.Where(rule => rule.Targets.Overlaps(identities)).ToList();
        if (required.Find(rule => rule.Rule is null) is { Rule: null, Name: { } name })
        {
            return new(ReactionBatchGapKind.Unrepresented, $"constraint '{name}' is not represented in the document")
            {
                RequiresAtomic = rejection is not null
            };
        }

        var declarations = required.Select(rule => rule.Rule).OfType<ConstraintModel>().Concat(events.SelectMany(ConstraintReader.FromEvent)).ToList();

        return declarations.Count == 0 ? null : new(ReactionBatchGapKind.Rejection, rejection ?? "the returned events have append constraints")
        {
            RequiresAtomic = rejection is not null,
            RequiredConstraints = declarations
        };
    }
}
