// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads what a specification says followed from the command it issued.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> every body is read through.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unreadable is reported to.</param>
/// <remarks>
/// Each assertion is one sentence about the outcome, and several of them routinely say the same sentence about a
/// different part of the same event - once for each value it carries. Screenplay says an event followed once, so a
/// sentence already said is passed over rather than repeated.
/// <para>
/// An assertion inherited from a base context is written wherever that context is, which need not be the project the
/// scenario is - so which model reads a body is asked rather than assumed.
/// </para>
/// </remarks>
public class SpecificationOutcomeReader(SemanticModels models, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Reads what a specification says followed.
    /// </summary>
    /// <param name="type">The type declaring the specification.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <param name="name">The name of the specification.</param>
    /// <param name="location">Where the specification lives, for use in diagnostics.</param>
    public void Read(INamedTypeSymbol type, SpecificationDraft draft, string name, string location)
    {
        foreach (var assertion in SpecificationMembers.AssertionsIn(type))
        {
            foreach (var body in HandlerBodies.Of(assertion))
            {
                if (models.For(body.SyntaxTree) is { } semanticModel)
                {
                    ReadBody(body, semanticModel, draft, name, location);
                }
            }
        }
    }

    /// <summary>
    /// Adds an event a specification says followed, or records that it cannot be read.
    /// </summary>
    /// <param name="appended">The type the assertion names.</param>
    /// <param name="source">The exact authored assertion location.</param>
    /// <param name="draft">The scenario collected so far.</param>
    static void AddEvent(ITypeSymbol appended, Location source, SpecificationDraft draft)
    {
        if (!EventReader.IsEvent(appended))
        {
            draft.CannotRead($"it expects '{appended.Name}' to follow, which is not declared as an event");
            return;
        }

        if (!draft.Then.Any(_ => string.Equals(_.Name, appended.Name, StringComparison.Ordinal)))
        {
            var state = new SpecificationStateModel(appended.Name, SpecificationStateKind.Event, []);
            draft.AddThen(state, appended, source);
        }
    }

    /// <summary>
    /// Reads what one assertion says followed.
    /// </summary>
    /// <param name="body">The body of the assertion.</param>
    /// <param name="semanticModel">The semantic model of the tree the body lives in.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <param name="name">The name of the specification.</param>
    /// <param name="location">Where the specification lives.</param>
    void ReadBody(SyntaxNode body, SemanticModel semanticModel, SpecificationDraft draft, string name, string location)
    {
        foreach (var invocation in body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            {
                continue;
            }

            var appended = SpecificationAssertions.AppendedEventOf(method);
            var rejection = SpecificationAssertions.IsRejection(invocation, method);
            if (appended is null && !rejection)
            {
                continue;
            }

            if (!StepsTaken.Always(invocation, body))
            {
                draft.CannotRead("what it expects to follow is only expected under a condition, and a scenario says what followed");
                return;
            }

            if (appended is not null)
            {
                AddEvent(appended, invocation.GetLocation(), draft);
                continue;
            }

            AddError(invocation, method, semanticModel, draft, name, location);
        }
    }

    /// <summary>
    /// Adds a rejection a specification says followed, named by the reason the source gives for it.
    /// </summary>
    /// <param name="invocation">The assertion to read.</param>
    /// <param name="method">The method being called.</param>
    /// <param name="semanticModel">The semantic model of the tree the assertion lives in.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <param name="name">The name of the specification.</param>
    /// <param name="location">Where the specification lives.</param>
    void AddError(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        SemanticModel semanticModel,
        SpecificationDraft draft,
        string name,
        string location)
    {
        var reason = SpecificationAssertions.IsNamedRejection(method)
            ? ReasonOf(invocation, semanticModel, name, location)
            : string.Empty;

        if (!draft.Errors.Contains(reason, StringComparer.Ordinal))
        {
            draft.AddError(reason, invocation.GetLocation());
        }
    }

    /// <summary>
    /// Gets the reason an assertion names for a rejection.
    /// </summary>
    /// <param name="invocation">The assertion to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the assertion lives in.</param>
    /// <param name="name">The name of the specification.</param>
    /// <param name="location">Where the specification lives.</param>
    /// <returns>The reason, empty when the source names one this cannot read.</returns>
    string ReasonOf(InvocationExpressionSyntax invocation, SemanticModel semanticModel, string name, string location)
    {
        var named = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
        if (named is not null && semanticModel.GetConstantValue(named) is { HasValue: true, Value: { } value })
        {
            return value.ToString() ?? string.Empty;
        }

        diagnostics.Information(
            ScreenplayDiagnosticCodes.UnreadableSpecificationValue,
            $"The reason '{name}' gives for a rejection is code rather than a constant, so the rejection is stated without one",
            location);

        return string.Empty;
    }
}
