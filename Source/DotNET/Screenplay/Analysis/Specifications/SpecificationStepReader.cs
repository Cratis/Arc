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
/// <param name="heldValues">The held values cached for the analysis.</param>
/// <remarks>
/// The steps are walked from the base of the chain down, and a base context is routinely written in a project below
/// the scenario inheriting it - so which model reads a body is asked rather than assumed.
/// </remarks>
public class SpecificationStepReader(SemanticModels models, SpecificationValues values, HeldValues? heldValues = null)
{
    readonly HeldValues _held = heldValues ?? new(models);

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

        foreach (var step in calls)
        {
            var admission = AdmitGiven(steps, step, draft);
            if (admission == StepAdmission.Stop)
            {
                return;
            }

            if (admission == StepAdmission.Read)
            {
                ReadGivenStep(steps, step, new(step.SemanticModel, draft, name, location));
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
        foreach (var step in CallsIn(steps, SpecificationMembers.BecauseMethod))
        {
            var append = SpecificationMembers.HoldsAnEventScenario(steps) && SpecificationCalls.IsAppendAction(step.Invocation, step.Method, step.SemanticModel);
            if (!append && !SpecificationCalls.IsExecution(step.Method))
            {
                continue;
            }

            var scope = new SpecificationStepScope(step.SemanticModel, draft, name, location);
            if (ReadAction(step, append, scope) == StepAdmission.Stop)
            {
                return;
            }
        }
    }

    static StepAdmission AdmitGiven(INamedTypeSymbol steps, Step step, SpecificationDraft draft)
    {
        var seeding = SpecificationCalls.IsSeedingTheEventLog(step.Method);
        if (!seeding && !IsGivenStep(step.Method))
        {
            return StepAdmission.Skip;
        }

        if (seeding && IsUnreachedSeeding(steps, step))
        {
            if (!SpecificationMembers.HoldsAScenario(steps))
            {
                return StepAdmission.Skip;
            }

            draft.CannotRead("it seeds an event log that this cannot tell the command it issues runs against");
            return StepAdmission.Stop;
        }

        if (step.Always)
        {
            return StepAdmission.Read;
        }

        draft.CannotRead("what it starts from only happens under a condition, and a scenario says what happened");
        return StepAdmission.Stop;
    }

    static bool IsGivenStep(IMethodSymbol method) => SpecificationCalls.IsGivenEvents(method) || SpecificationCalls.IsGivenReadModel(method);

    static bool IsUnreachedSeeding(INamedTypeSymbol steps, Step step) =>
        (!SpecificationMembers.HoldsAnEventScenario(steps) || SpecificationMembers.HoldsAScenario(steps)) &&
        !SpecificationCalls.IsReachedThroughACommandScenario(step.Invocation, step.SemanticModel);

    static bool HoldsAnyScenario(INamedTypeSymbol steps) => SpecificationMembers.HoldsAnEventScenario(steps) || SpecificationMembers.HoldsAScenario(steps);

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

    static string UnreadableActionMessage(bool append) => append
        ? "the event it appends is put together somewhere this cannot read"
        : "the command it issues is put together somewhere this cannot read";

    static string? ActionFailure(SpecificationActionStep action)
    {
        if (!action.Step.Always)
        {
            return action.Append
                ? "the event it appends is only appended under a condition, and a scenario says what happened"
                : "the command it issues is only issued under a condition, and a scenario says what happened";
        }

        return action.Scope.Draft.When is not null ? "it performs more than one action, and a scenario is about one" : null;
    }

    static string? ActionTypeFailure(SpecificationActionStep action, INamedTypeSymbol command)
    {
        var append = action.Append;
        if (append && CallArguments.For(action.Step.Invocation, action.Step.Method, "additionalEvents").Any())
        {
            return "it appends several events as its action, and a scenario holds one append";
        }

        return (append ? !EventReader.IsEvent(command) : !CommandReader.IsCommand(command))
            ? $"'{command.Name}' is not a {(append ? "event" : "command")} the document declares"
            : null;
    }

    SpecificationIssuedCommand? ConstructedCommand(SpecificationActionStep action)
    {
        var step = action.Step;
        var stated = CallArguments.For(step.Invocation, step.Method, SpecificationCalls.PayloadParameterOf(step.Method) ?? string.Empty).ToList();
        if (stated is not [var issued] || _held.ConstructionOf(issued, action.Scope.SemanticModel) is not { } construction)
        {
            return null;
        }

        return construction.SemanticModel.GetTypeInfo(construction.Creation).Type is INamedTypeSymbol command
            ? new SpecificationIssuedCommand(issued, construction, command)
            : null;
    }

    void ReadCommandSources(SpecificationIssuedCommand issued, SpecificationStepScope scope) =>
        scope.Draft.EventSources.ReadCommand(
            issued.Command,
            issued.Construction.Creation,
            issued.Construction.SemanticModel,
            models,
            scope.SemanticModel.GetSymbolInfo(MappingSourceReader.Unwrap(issued.Issued)).Symbol,
            scope.SemanticModel.Compilation);

    void SetWhen(SpecificationActionStep action, SpecificationIssuedCommand issued)
    {
        var (step, append, scope) = action;
        var draft = scope.Draft;
        var route = append ? SpecificationEventRoutes.Read(step.Invocation, step.Method, scope.SemanticModel, draft) : null;
        var state = new SpecificationStateModel(
            issued.Command.Name,
            append ? SpecificationStateKind.Event : SpecificationStateKind.Command,
            values.Read(issued.Construction.Creation, issued.Construction.SemanticModel, issued.Command, scope.Name, scope.Location, draft))
        {
            For = append ? draft.EventSources.Read(step.Invocation, step.Method, scope.SemanticModel, draft, requireLiteral: route is { Source: not null }) : null,
            Route = route
        };
        draft.SetWhen(state, issued.Command, step.Invocation.GetLocation());
    }

    void ReadGivenStep(INamedTypeSymbol steps, Step step, SpecificationStepScope scope)
    {
        var draft = scope.Draft;
        var kind = SpecificationCalls.IsGivenReadModel(step.Method) ? SpecificationStateKind.ReadModel : SpecificationStateKind.Event;
        var route = kind == SpecificationStateKind.Event ? SpecificationEventRoutes.Read(step.Invocation, step.Method, scope.SemanticModel, draft) : null;
        var source = kind == SpecificationStateKind.Event && (draft.When is not null || HoldsAnyScenario(steps))
            ? draft.EventSources.Read(step.Invocation, step.Method, scope.SemanticModel, draft, requireLiteral: route is { Source: not null })
            : null;

        foreach (var stated in CallArguments.For(step.Invocation, step.Method, SpecificationCalls.PayloadParameterOf(step.Method) ?? string.Empty))
        {
            Add(stated, scope, new(kind, source, route));
        }
    }

    StepAdmission ReadAction(Step step, bool append, SpecificationStepScope scope)
    {
        var action = new SpecificationActionStep(step, append, scope);
        var draft = scope.Draft;
        var failure = ActionFailure(action);
        var issued = failure is null ? ConstructedCommand(action) : null;
        failure ??= issued is null ? UnreadableActionMessage(append) : ActionTypeFailure(action, issued.Command);
        if (failure is not null)
        {
            draft.CannotRead(failure);
            return StepAdmission.Stop;
        }

        if (!append)
        {
            ReadCommandSources(issued!, scope);
        }

        SetWhen(action, issued!);
        return StepAdmission.Read;
    }

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
    /// <param name="stated">The expression stating it.</param>
    /// <param name="scope">What the call stating it is read against.</param>
    /// <param name="given">What the state is.</param>
    void Add(ExpressionSyntax stated, SpecificationStepScope scope, SpecificationGivenState given)
    {
        var (semanticModel, draft, name, location) = scope;
        var (kind, source, route) = given;
        if (_held.ConstructionOf(stated, semanticModel) is not { } construction ||
            construction.SemanticModel.GetTypeInfo(construction.Creation).Type is not INamedTypeSymbol type)
        {
            draft.CannotRead("what it starts from is put together somewhere this cannot read");
            return;
        }

        if (kind == SpecificationStateKind.Event && !EventReader.IsEvent(type))
        {
            draft.CannotRead($"it starts from '{type.Name}', which is not declared as an event");
            return;
        }

        var state = new SpecificationStateModel(
            type.Name,
            kind,
            values.Read(construction.Creation, construction.SemanticModel, type, name, location, draft)) { For = source, Route = route };
        draft.AddGiven(state, type, stated.GetLocation());
    }
}
