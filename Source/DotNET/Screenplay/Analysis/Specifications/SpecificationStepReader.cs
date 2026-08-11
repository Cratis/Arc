// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads what a specification starts from and the command it issues, from the bodies stating them.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> every body is read through.</param>
/// <param name="values">The <see cref="SpecificationValues"/> reading the values each step states.</param>
/// <remarks>
/// The steps are walked from the base of the chain down, and a base context is routinely written in a project below
/// the scenario inheriting it - so which model reads a body is asked rather than assumed.
/// </remarks>
public class SpecificationStepReader(SemanticModels models, SpecificationValues values)
{
    /// <summary>
    /// Reads what a specification had already seen when it issued its command.
    /// </summary>
    /// <param name="steps">The type the steps are written on.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <param name="name">The name of the specification.</param>
    /// <param name="location">Where the specification lives, for use in diagnostics.</param>
    /// <param name="alsoWhereTheActionIs">Whether to read the method performing the action as well.</param>
    /// <remarks>
    /// Where the world a scenario starts from is written depends on what the scenario is. One issuing a command sets
    /// the world up first and issues the command as its action, so the world is in <c>Establish</c> and reading the
    /// action would take the command's own events for events that had already happened. One about a read model has no
    /// command: the events are the action, and are routinely written straight into it. So the action is read only
    /// when there is no command to confuse them with.
    /// </remarks>
    public void ReadGiven(INamedTypeSymbol steps, SpecificationDraft draft, string name, string location, bool alsoWhereTheActionIs = false)
    {
        var calls = alsoWhereTheActionIs
            ? CallsIn(steps, SpecificationMembers.EstablishMethod).Concat(CallsIn(steps, SpecificationMembers.BecauseMethod))
            : CallsIn(steps, SpecificationMembers.EstablishMethod);

        foreach (var (invocation, method, semanticModel, always) in calls)
        {
            if (!SpecificationCalls.IsGivenEvents(method) && !SpecificationCalls.IsGivenReadModel(method))
            {
                continue;
            }

            if (!always)
            {
                draft.CannotRead("what it starts from only happens under a condition, and a scenario says what happened");
                return;
            }

            var kind = SpecificationCalls.IsGivenReadModel(method) ? SpecificationStateKind.ReadModel : SpecificationStateKind.Event;

            foreach (var stated in CallArguments.For(invocation, method, SpecificationCalls.PayloadParameterOf(method) ?? string.Empty))
            {
                Add(draft.Given, stated, kind, semanticModel, draft, name, location);
            }
        }
    }

    /// <summary>
    /// Reads the command a specification issues.
    /// </summary>
    /// <param name="steps">The type the steps are written on.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <param name="name">The name of the specification.</param>
    /// <param name="location">Where the specification lives, for use in diagnostics.</param>
    public void ReadWhen(INamedTypeSymbol steps, SpecificationDraft draft, string name, string location)
    {
        foreach (var (invocation, method, semanticModel, always) in CallsIn(steps, SpecificationMembers.BecauseMethod))
        {
            if (!SpecificationCalls.IsExecution(method))
            {
                continue;
            }

            if (!always)
            {
                draft.CannotRead("the command it issues is only issued under a condition, and a scenario says what happened");
                return;
            }

            if (draft.When is not null)
            {
                draft.CannotRead("it issues more than one command, and a scenario is about one");
                return;
            }

            var stated = CallArguments.For(invocation, method, SpecificationCalls.PayloadParameterOf(method) ?? string.Empty).ToList();
            if (stated is not [BaseObjectCreationExpressionSyntax creation] ||
                semanticModel.GetTypeInfo(creation).Type is not INamedTypeSymbol command)
            {
                draft.CannotRead("the command it issues is put together somewhere this cannot read");
                return;
            }

            if (!CommandReader.IsCommand(command))
            {
                draft.CannotRead($"'{command.Name}' is not a command the document declares");
                return;
            }

            draft.When = new(command.Name, SpecificationStateKind.Command, values.Read(creation, semanticModel, command, name, location));
        }
    }

    /// <summary>
    /// Gets every call a body makes that resolves to a method.
    /// </summary>
    /// <param name="body">The body to walk.</param>
    /// <param name="semanticModel">The semantic model of the tree the body lives in.</param>
    /// <returns>The calls, in the order the body makes them.</returns>
    static IEnumerable<Step> Calls(SyntaxNode body, SemanticModel semanticModel) =>
        body.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => (invocation, Method: semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol, semanticModel, body))
            .Where(call => call.Method is not null)
            .Select(call => new Step(call.invocation, call.Method!, call.semanticModel, StepsTaken.Always(call.invocation, call.body)));

    /// <summary>
    /// Gets every call a method of a chain makes, with the model the tree it lives in is read through.
    /// </summary>
    /// <param name="type">The type to walk from.</param>
    /// <param name="method">The name of the method to read.</param>
    /// <returns>The calls, from the base down and in the order each body makes them.</returns>
    IEnumerable<Step> CallsIn(INamedTypeSymbol type, string method) =>
        SpecificationMembers.MethodsIn(type, method)
            .SelectMany(HandlerBodies.Of)
            .Select(body => (body, model: models.For(body.SyntaxTree)))
            .Where(read => read.model is not null)
            .SelectMany(read => Calls(read.body, read.model!));

    /// <summary>
    /// Adds one state a specification starts from, or records that it cannot be read.
    /// </summary>
    /// <param name="states">The states collected so far.</param>
    /// <param name="stated">The expression stating it.</param>
    /// <param name="kind">What the state is.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <param name="name">The name of the specification.</param>
    /// <param name="location">Where the specification lives.</param>
    void Add(
        IList<SpecificationStateModel> states,
        ExpressionSyntax stated,
        SpecificationStateKind kind,
        SemanticModel semanticModel,
        SpecificationDraft draft,
        string name,
        string location)
    {
        if (stated is not BaseObjectCreationExpressionSyntax creation ||
            semanticModel.GetTypeInfo(creation).Type is not INamedTypeSymbol type)
        {
            draft.CannotRead("what it starts from is put together somewhere this cannot read");
            return;
        }

        if (kind == SpecificationStateKind.Event && !EventReader.IsEvent(type))
        {
            draft.CannotRead($"it starts from '{type.Name}', which is not declared as an event");
            return;
        }

        states.Add(new(type.Name, kind, values.Read(creation, semanticModel, type, name, location)));
    }
}
