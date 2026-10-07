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
    /// <remarks>
    /// A command that was rejected appended nothing, so an assertion that the log holds an event the scenario started
    /// with is about what had already happened and not about what followed. Whether the command was rejected is
    /// said by an assertion that may be written anywhere among them, so it is asked before any of them is read.
    /// </remarks>
    public void Read(INamedTypeSymbol type, SpecificationDraft draft, string name, string location)
    {
        var bodies = SpecificationMembers.AssertionsIn(type)
            .SelectMany(HandlerBodies.Of)
            .Select(body => (Body: body, Model: models.For(body.SyntaxTree)))
            .Where(_ => _.Model is not null)
            .ToList();

        var rejected = bodies.Exists(_ => Rejects(_.Body, _.Model!));
        var rejections = bodies.SelectMany(item => item.Body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
            .Select(invocation => (Invocation: invocation, Method: item.Model!.GetSymbolInfo(invocation).Symbol as IMethodSymbol)))
            .Where(item => item.Method is not null && SpecificationAssertions.IsRejection(item.Invocation, item.Method))
            .ToList();
        var sourceIndependent = new[] { "ShouldHaveValidationErrors", "ShouldHaveValidationErrorBecauseOf", "ShouldHaveValidationErrorFor", "ShouldNotBeAuthorized" };
        draft.HasOnlySourceIndependentRejections = rejections.Exists(item => sourceIndependent.Contains(item.Method!.Name, StringComparer.Ordinal)) &&
            rejections.TrueForAll(item => sourceIndependent.Contains(item.Method!.Name, StringComparer.Ordinal) ||
                string.Equals(item.Method.Name, "ShouldNotBeSuccessful", StringComparison.Ordinal) ||
                string.Equals(item.Method.Name, "ShouldBeFalse", StringComparison.Ordinal));
        draft.AssertsResponse = bodies.Exists(item => item.Body.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().Any(member =>
            item.Model!.GetSymbolInfo(member).Symbol is IPropertySymbol property && string.Equals(property.Name, "Response", StringComparison.Ordinal) &&
            (property.ContainingType.Is("Cratis.Arc.Commands.CommandResult") || property.ContainingType.FindBase("Cratis.Arc.Commands.CommandResult") is not null)));

        foreach (var (body, semanticModel) in bodies)
        {
            ReadBody(body, semanticModel!, draft, name, location, rejected);
        }
    }

    /// <summary>
    /// Determines whether an expectation restates the append action rather than a fact following it.
    /// </summary>
    /// <param name="state">The expected fact.</param>
    /// <param name="action">The scenario action.</param>
    /// <returns>Whether the expectation names the appended event type.</returns>
    internal static bool RestatesAppend(SpecificationStateModel state, SpecificationStateModel? action) =>
        action is { Kind: SpecificationStateKind.Event } && state.Kind == SpecificationStateKind.Event &&
        string.Equals(state.Name, action.Name, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether an event is one the scenario started with.
    /// </summary>
    /// <param name="appended">The type the assertion names.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <returns>True when the scenario states an event of that type as already happened.</returns>
    static bool StartedWith(ITypeSymbol appended, SpecificationDraft draft) =>
        draft.Given.Any(_ => _.Kind == SpecificationStateKind.Event && string.Equals(_.Name, appended.Name, StringComparison.Ordinal));

    /// <summary>
    /// Determines whether a body asserts that the command was rejected.
    /// </summary>
    /// <param name="body">The body of the assertion.</param>
    /// <param name="semanticModel">The semantic model of the tree the body lives in.</param>
    /// <returns>True when the body holds a rejection.</returns>
    static bool Rejects(SyntaxNode body, SemanticModel semanticModel) =>
        body.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation =>
                semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
                SpecificationAssertions.IsRejection(invocation, method));

    /// <summary>
    /// Adds an event a specification says followed, or records that it cannot be read.
    /// </summary>
    /// <param name="appended">The type the assertion names.</param>
    /// <param name="invocation">The appended-event assertion.</param>
    /// <param name="method">The exactly bound assertion method.</param>
    /// <param name="semanticModel">The semantic model owning the assertion.</param>
    /// <param name="draft">The scenario collected so far.</param>
    static void AddEvent(
        ITypeSymbol appended,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        SemanticModel semanticModel,
        SpecificationDraft draft)
    {
        if (!EventReader.IsEvent(appended))
        {
            draft.CannotRead($"it expects '{appended.Name}' to follow, which is not declared as an event");
            return;
        }

        if (draft.Then.Any(_ => string.Equals(_.Name, appended.Name, StringComparison.Ordinal)))
        {
            draft.CannotRead($"it expects '{appended.Name}' more than once, and repeated event expectations are ambiguous");
            return;
        }

        if (!SpecificationEventPredicateValues.TryRead(
                invocation,
                method,
                appended,
                semanticModel,
                draft,
                out var values,
                out var reason))
        {
            draft.CannotRead(reason!);
            return;
        }

        var state = new SpecificationStateModel(appended.Name, SpecificationStateKind.Event, values)
        {
            For = draft.When is not null
                ? draft.EventSources.Read(invocation, method, semanticModel, draft)
                : null
        };
        draft.AddThen(state, appended, invocation.GetLocation());
    }

    /// <summary>
    /// Reads what one assertion says followed.
    /// </summary>
    /// <param name="body">The body of the assertion.</param>
    /// <param name="semanticModel">The semantic model of the tree the body lives in.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <param name="name">The name of the specification.</param>
    /// <param name="location">Where the specification lives.</param>
    /// <param name="rejected">Whether an assertion of the specification says the command was rejected.</param>
    void ReadBody(SyntaxNode body, SemanticModel semanticModel, SpecificationDraft draft, string name, string location, bool rejected)
    {
        foreach (var invocation in body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            {
                continue;
            }

            var appended = SpecificationAssertions.AppendedEventOf(method);
            var rejection = SpecificationAssertions.IsRejection(invocation, method);
            if (appended is null && SpecificationAssertions.HasAppendedEventAssertionName(method))
            {
                draft.CannotRead("an appended-event assertion does not match an exact allowlisted testing API signature");
                return;
            }

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
                if (rejected && StartedWith(appended, draft))
                {
                    continue;
                }

                AddEvent(appended, invocation, method, semanticModel, draft);
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
