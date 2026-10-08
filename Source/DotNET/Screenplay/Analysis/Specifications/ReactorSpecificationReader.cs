// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Analysis.Reactors;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads a reactor scenario as a specification of the declarative reaction the reactor is stated as.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> every body is read through.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unreadable is reported to.</param>
/// <param name="heldValues">The held values cached for the analysis.</param>
/// <remarks>
/// A reactor scenario hands an event to a reactor and records what the reactor returned. When the reactor is stated
/// as a declarative reaction that is exactly what a specification appending the event says: <c>when append</c> the
/// delivered event, <c>then</c> the events the reaction appended. It is only read that way when it says nothing else.
/// A scenario asserting what a collaborator was asked to do, asserting the command a reaction invokes rather than the
/// facts that command records, or written against a reactor whose handler stays code has no counterpart, and is
/// reported as such. A scenario that has a counterpart but states something this cannot read is left out as
/// unreadable.
/// <para>
/// Every event a reactor scenario delivers runs the reactor, so only the last one can be the append. An earlier one is
/// what had already happened only when the reactor does not handle it; otherwise its side effects are among the ones
/// the scenario recorded, which a scenario about one append cannot hold.
/// </para>
/// </remarks>
public class ReactorSpecificationReader(SemanticModels models, ScreenplayDiagnostics diagnostics, HeldValues? heldValues = null)
{
    /// <summary>The assertion naming an event or command the reactor returned.</summary>
    public const string ProducedAssertion = "ShouldHaveProduced";

    /// <summary>The assertion naming an event or command the reactor did not return.</summary>
    public const string NotProducedAssertion = "ShouldNotHaveProduced";

    readonly HeldValues _held = heldValues ?? new(models);

    /// <summary>
    /// Gets the reactor a type is written as a scenario of, when it is written the way a specification is.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>The reactor, or <see langword="null"/> when the type is not a reactor scenario.</returns>
    public static INamedTypeSymbol? ReactorOf(INamedTypeSymbol type) =>
        type is { TypeKind: TypeKind.Class, IsAbstract: false, ContainingType: null } &&
        SpecificationMembers.StepsOf(type) is var steps &&
        SpecificationMembers.MethodsIn(steps, SpecificationMembers.BecauseMethod).Any()
            ? SpecificationMembers.ReactorOf(steps)
            : null;

    /// <summary>
    /// Determines why a reactor scenario has no counterpart in the language.
    /// </summary>
    /// <param name="type">The type declaring the scenario.</param>
    /// <param name="reactor">The reactor it is a scenario of.</param>
    /// <returns>The reason, or <see langword="null"/> when the scenario has a counterpart.</returns>
    public string? WithoutCounterpart(INamedTypeSymbol type, INamedTypeSymbol reactor)
    {
        if (!ReactorReader.ReactionsOf(reactor, models).Any())
        {
            return $"of '{reactor.Name}', whose handlers are code rather than declarative reactions";
        }

        foreach (var (invocation, method, _) in Assertions(type))
        {
            if (method is null || !IsReactorAssertion(method))
            {
                return $"that asserts something other than what '{reactor.Name}' returned, such as what a collaborator was asked to do ('{invocation.Expression}')";
            }

            if (method.TypeArguments is [var asserted] && CommandReader.IsCommand(asserted) && method.Name == ProducedAssertion)
            {
                return $"that asserts '{reactor.Name}' invokes '{asserted.Name}', while a specification states the facts the invoked command records";
            }
        }

        return null;
    }

    /// <summary>
    /// Reads a reactor scenario.
    /// </summary>
    /// <param name="type">The type declaring the scenario.</param>
    /// <param name="reactor">The reactor it is a scenario of.</param>
    /// <param name="name">The name the specification is declared under.</param>
    /// <returns>The <see cref="SpecificationModel"/>, or <see langword="null"/> when the scenario cannot be read.</returns>
    public SpecificationModel? Read(INamedTypeSymbol type, INamedTypeSymbol reactor, string name)
    {
        var location = type.ToDisplayString();
        var steps = SpecificationMembers.StepsOf(type);
        var draft = new SpecificationDraft { EventSources = new(models, _held) };
        var stated = new ScreenplayDiagnostics();
        var values = new SpecificationValues(stated, new GeneratedIdentities(models));
        var reactions = ReactorReader.ReactionsOf(reactor, models).ToList();
        var observed = ReactorReader.ObservedBy(reactor).ToHashSet(StringComparer.Ordinal);

        var delivered = ReadDeliveries(steps, draft);
        ReactionModel? reaction = null;
        if (draft.Unreadable is null)
        {
            reaction = ReadAppend(delivered, reactions, observed, values, draft, name, location);
        }

        if (draft.Unreadable is null && reaction is not null)
        {
            ReadOutcome(type, reaction, draft);
        }

        if (draft.Unreadable is not null)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnreadableSpecification,
                $"The scenario '{name}' was left out because {draft.Unreadable}",
                location);

            return null;
        }

        diagnostics.AddRange(stated.All);

        var order = reaction!.Produces.Select(_ => _.EventName).ToList();
        var then = draft.Then.OrderBy(_ => order.IndexOf(_.Name)).ToList();
        var specification = new SpecificationModel(name, [.. draft.Given], draft.When, then, []) { Reactor = reactor.Name };
        SpecificationEvidence.Register(
            specification,
            new(
                type,
                type.Locations.First(_ => _.IsInSource),
                draft.GetStateEvidence(),
                draft.GetValueEvidence(),
                draft.GetErrorEvidence(),
                [.. stated.All]));

        return specification;
    }

    /// <summary>
    /// Determines whether a state gives every property of its event a value.
    /// </summary>
    /// <param name="state">The state.</param>
    /// <param name="event">The event it is of.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <returns>True when it does.</returns>
    /// <remarks>
    /// A specification states an event whole - every value it carries, exactly once - so an event a scenario only
    /// partly states, whether it delivers it or expects it, cannot be written.
    /// </remarks>
    static bool StatesEveryValue(SpecificationStateModel state, ITypeSymbol @event, SpecificationDraft draft)
    {
        var stated = state.Values.Select(_ => _.Property).ToHashSet(StringComparer.Ordinal);
        if (@event.DeclaredProperties().All(_ => stated.Contains(_.Name)))
        {
            return true;
        }

        draft.CannotRead($"it does not state every value of '{@event.Name}', and a specification states an event whole");
        return false;
    }

    /// <summary>
    /// Determines whether a method is an assertion on what a reactor scenario recorded.
    /// </summary>
    /// <param name="method">The method being called.</param>
    /// <returns>True when it is one.</returns>
    static bool IsReactorAssertion(IMethodSymbol method) =>
        (string.Equals(method.Name, ProducedAssertion, StringComparison.Ordinal) || string.Equals(method.Name, NotProducedAssertion, StringComparison.Ordinal)) &&
        method.ContainingType.OriginalDefinition.FullMetadataName() == WellKnownTypeNames.ReactorScenario;

    /// <summary>
    /// Determines whether a call delivers events through a reactor scenario.
    /// </summary>
    /// <param name="method">The method being called.</param>
    /// <returns>True when it does.</returns>
    static bool IsDelivery(IMethodSymbol method) =>
        method.Name == SpecificationCalls.EventsMethod &&
        method.ContainingType.OriginalDefinition.FullMetadataName() == WellKnownTypeNames.ReactorSourceGivenBuilder;

    /// <summary>
    /// Determines whether a call pins a read model through a reactor scenario.
    /// </summary>
    /// <param name="method">The method being called.</param>
    /// <returns>True when it does.</returns>
    static bool IsReadModelSeed(IMethodSymbol method) =>
        method.Name == SpecificationCalls.ReadModelMethod &&
        method.ContainingType.OriginalDefinition.FullMetadataName() == WellKnownTypeNames.ReactorSourceGivenBuilder;

    /// <summary>
    /// Gets every call a method of the chain of a scenario makes.
    /// </summary>
    /// <param name="steps">The type the steps are written on.</param>
    /// <param name="method">The name of the method to read.</param>
    /// <returns>The calls, from the base down and in the order each body makes them.</returns>
    IEnumerable<Step> CallsIn(INamedTypeSymbol steps, string method) =>
        SpecificationMembers.MethodsIn(steps, method)
            .SelectMany(HandlerBodies.Of)
            .Select(body => (Body: body, Model: models.For(body.SyntaxTree)))
            .Where(_ => _.Model is not null)
            .SelectMany(read => read.Body.DescendantNodesAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .Select(invocation => (Invocation: invocation, Method: read.Model!.GetSymbolInfo(invocation).Symbol as IMethodSymbol))
                .Where(_ => _.Method is not null)
                .Select(_ => new Step(_.Invocation, _.Method!, read.Model!, StepsTaken.Always(_.Invocation, read.Body))));

    /// <summary>
    /// Gets every call the assertions of a scenario make.
    /// </summary>
    /// <param name="type">The type declaring the scenario.</param>
    /// <returns>The calls, with the method each resolves to and the model it is read through.</returns>
    IEnumerable<(InvocationExpressionSyntax Invocation, IMethodSymbol? Method, SemanticModel Model)> Assertions(INamedTypeSymbol type) =>
        SpecificationMembers.AssertionsIn(type)
            .SelectMany(HandlerBodies.Of)
            .Select(body => (Body: body, Model: models.For(body.SyntaxTree)))
            .Where(_ => _.Model is not null)
            .SelectMany(read => read.Body.DescendantNodesAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .Where(invocation => invocation.FirstAncestorOrSelf<LambdaExpressionSyntax>() is null)
                .Select(invocation => (invocation, read.Model!.GetSymbolInfo(invocation).Symbol as IMethodSymbol, read.Model!)));

    /// <summary>
    /// Reads every event the scenario delivers to the reactor, in the order it delivers them.
    /// </summary>
    /// <param name="steps">The type the steps are written on.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <returns>The deliveries, with the call delivering each.</returns>
    List<(ExpressionSyntax Stated, Step Call)> ReadDeliveries(INamedTypeSymbol steps, SpecificationDraft draft)
    {
        var delivered = new List<(ExpressionSyntax, Step)>();
        foreach (var call in CallsIn(steps, SpecificationMembers.EstablishMethod).Concat(CallsIn(steps, SpecificationMembers.BecauseMethod)))
        {
            if (IsReadModelSeed(call.Method))
            {
                draft.CannotRead("it seeds a read model for the reactor, and a declarative reaction reads none");
                return delivered;
            }

            if (!IsDelivery(call.Method))
            {
                continue;
            }

            if (!call.Always)
            {
                draft.CannotRead("what it delivers to the reactor is only delivered under a condition, and a scenario says what happened");
                return delivered;
            }

            delivered.AddRange(CallArguments.For(call.Invocation, call.Method, SpecificationCalls.EventsParameter).Select(stated => (stated, call)));
        }

        if (delivered.Count == 0)
        {
            draft.CannotRead("the event it delivers to the reactor is put together somewhere this cannot read");
        }

        return delivered;
    }

    /// <summary>
    /// Reads what had happened and the append the scenario is about.
    /// </summary>
    /// <param name="delivered">The events the scenario delivers.</param>
    /// <param name="reactions">The declarative reactions of the reactor.</param>
    /// <param name="observed">The names of the events the reactor observes.</param>
    /// <param name="values">The reader of the values a construction states.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <param name="name">The name of the specification.</param>
    /// <param name="location">Where the specification lives.</param>
    /// <returns>The reaction to the appended event, or <see langword="null"/> when the scenario cannot be read.</returns>
    ReactionModel? ReadAppend(
        List<(ExpressionSyntax Stated, Step Call)> delivered,
        List<ReactionModel> reactions,
        HashSet<string> observed,
        SpecificationValues values,
        SpecificationDraft draft,
        string name,
        string location)
    {
        ReactionModel? reaction = null;
        for (var index = 0; index < delivered.Count; index++)
        {
            var (stated, call) = delivered[index];
            if (_held.ConstructionOf(stated, call.SemanticModel) is not { } construction ||
                construction.SemanticModel.GetTypeInfo(construction.Creation).Type is not INamedTypeSymbol @event)
            {
                draft.CannotRead("an event it delivers to the reactor is put together somewhere this cannot read");
                return null;
            }

            if (!EventReader.IsEvent(@event))
            {
                draft.CannotRead($"it delivers '{@event.Name}', which is not declared as an event");
                return null;
            }

            var last = index == delivered.Count - 1;
            if (!last && observed.Contains(@event.Name))
            {
                draft.CannotRead($"it delivers '{@event.Name}' to the reactor before the event it is about, and the reactor handles both, while a scenario is about one append");
                return null;
            }

            if (last && (reaction = reactions.SingleOrDefault(_ => _.EventName == @event.Name)) is null)
            {
                draft.CannotRead($"the reactor handles '{@event.Name}' with code rather than a declarative reaction");
                return null;
            }

            var source = draft.EventSources.Read(call.Invocation, call.Method, call.SemanticModel, draft);
            var state = new SpecificationStateModel(
                @event.Name,
                SpecificationStateKind.Event,
                [.. values.Read(construction.Creation, construction.SemanticModel, @event, name, location, draft)]) { For = source };
            if (!StatesEveryValue(state, @event, draft))
            {
                return null;
            }

            if (last)
            {
                draft.SetWhen(state, @event, stated.GetLocation());
            }
            else
            {
                draft.AddGiven(state, @event, stated.GetLocation());
            }
        }

        return reaction;
    }

    /// <summary>
    /// Reads what the scenario says the reaction appended.
    /// </summary>
    /// <param name="type">The type declaring the scenario.</param>
    /// <param name="reaction">The reaction to the appended event.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <remarks>
    /// After an append, a specification states every fact that followed. The scenario is only kept when what it says
    /// the reactor returned is everything the reaction appends, each event once, so the document never states a
    /// scenario that holds less than the reaction does.
    /// </remarks>
    void ReadOutcome(INamedTypeSymbol type, ReactionModel reaction, SpecificationDraft draft)
    {
        foreach (var (invocation, method, semanticModel) in Assertions(type))
        {
            if (method is null || !IsReactorAssertion(method) || method.TypeArguments is not [var asserted])
            {
                continue;
            }

            if (!StepsTaken.Always(invocation, invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>() ?? (SyntaxNode)invocation))
            {
                draft.CannotRead("what it expects the reactor to return is only expected under a condition, and a scenario says what followed");
                return;
            }

            if (method.Name == NotProducedAssertion)
            {
                if (reaction.Produces.Any(_ => _.EventName == asserted.Name) || reaction.Invokes.Any(_ => _.CommandName == asserted.Name))
                {
                    draft.CannotRead($"it expects the reactor not to return '{asserted.Name}', which its reaction does");
                    return;
                }

                continue;
            }

            if (draft.Then.Any(_ => _.Name == asserted.Name))
            {
                draft.CannotRead($"it expects '{asserted.Name}' more than once, and repeated event expectations are ambiguous");
                return;
            }

            if (!SpecificationEventPredicateValues.TryRead(invocation, method, asserted, semanticModel, draft, out var values, out var reason))
            {
                draft.CannotRead(reason!);
                return;
            }

            var state = new SpecificationStateModel(asserted.Name, SpecificationStateKind.Event, values) { For = draft.When?.For };
            if (!StatesEveryValue(state, asserted, draft))
            {
                return;
            }

            draft.AddThen(state, asserted, invocation.GetLocation());
        }

        var expected = draft.Then.Select(_ => _.Name).Order(StringComparer.Ordinal);
        var produced = reaction.Produces.Select(_ => _.EventName).Order(StringComparer.Ordinal);
        if (reaction.Invokes.Any())
        {
            draft.CannotRead("its reaction invokes a command, and the facts that command records are not what the scenario states");
        }
        else if (!expected.SequenceEqual(produced, StringComparer.Ordinal))
        {
            draft.CannotRead("it does not state every event the reaction appends, each once, and a specification states every fact that follows the append");
        }
    }
}
