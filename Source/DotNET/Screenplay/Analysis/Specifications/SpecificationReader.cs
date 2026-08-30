// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads the scenario one type specifies a slice by.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> every body is read through.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unreadable is reported to.</param>
/// <remarks>
/// Only a specification driving a command through the real pipeline is read. A unit level one stands a collaborator
/// up behind a substitute and says what that collaborator was asked to do, which is a statement about the inside of
/// the slice rather than about its behavior, and Screenplay has nowhere to put it. Whether a specification is one or
/// the other is decided by what it touches: holding a scenario the pipeline runs in, or reaching the event log, is
/// what an integration specification does and nothing else does.
/// </remarks>
public class SpecificationReader(SemanticModels models, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Determines whether a type specifies a slice by driving a command through the pipeline.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is a specification of the kind this reads.</returns>
    public bool IsSpecification(INamedTypeSymbol type)
    {
        if (!IsWrittenAsOne(type))
        {
            return false;
        }

        var steps = SpecificationMembers.StepsOf(type);

        return SpecificationMembers.HoldsAScenario(steps) ||
            SpecificationMembers.ReadModelOf(steps) is not null ||
            DrivesASlice(steps);
    }

    /// <summary>
    /// Gets the scenario a type specifies a slice by that Screenplay has no way to hold.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>The name of the scenario, or <see langword="null"/> when there is nothing to report.</returns>
    /// <remarks>
    /// A specification holding one of these is specifying the slice as much as any other - what it does is real, and
    /// leaving it out without a word is the one thing the catalogue of codes exists to prevent. Two of the four
    /// scenarios an application is written with have nowhere to go: a scenario appending an event states the append
    /// itself as its action, and a <c>when</c> names a command and nothing else; a scenario driving a reactor says
    /// what a collaborator was asked to do, which is a statement about the inside of the slice. Both are said rather
    /// than recovered, because a document quietly missing four specifications in ten reads exactly like an
    /// application that has none.
    /// </remarks>
    public string? ScenarioWithoutCounterpart(INamedTypeSymbol type)
    {
        if (!IsWrittenAsOne(type) || IsSpecification(type))
        {
            return null;
        }

        return SpecificationMembers.ScenarioWithoutCounterpart(SpecificationMembers.StepsOf(type));
    }

    /// <summary>
    /// Reads the scenario a type specifies a slice by.
    /// </summary>
    /// <param name="type">The type declaring the specification.</param>
    /// <param name="name">The name the specification is declared under.</param>
    /// <returns>The <see cref="SpecificationModel"/>, or <see langword="null"/> when the scenario cannot be read.</returns>
    /// <remarks>
    /// What was left out of a scenario is held back until the scenario is known to survive. Saying that a value was
    /// left out of a scenario that was itself left out describes a document that says something it does not say, and
    /// there are far more values than scenarios - so the noise would be the loudest thing in the report.
    /// </remarks>
    public SpecificationModel? Read(INamedTypeSymbol type, string name)
    {
        var location = type.ToDisplayString();
        var steps = SpecificationMembers.StepsOf(type);
        var readModel = SpecificationMembers.ReadModelOf(steps);
        var draft = new SpecificationDraft();
        var stated = new ScreenplayDiagnostics();

        var reader = new SpecificationStepReader(models, new(stated, new GeneratedIdentities(models)));
        reader.ReadGiven(steps, draft, name, location, alsoWhereTheActionIs: readModel is not null);
        reader.ReadWhen(steps, draft, name, location);
        new SpecificationOutcomeReader(models, stated).Read(type, draft, name, location);

        if (readModel is INamedTypeSymbol namedReadModel && draft.When is null)
        {
            if (SpecificationReadModelOutcomeValues.TryRead(
                    type,
                    steps,
                    namedReadModel,
                    models,
                    draft,
                    out var values,
                    out var source,
                    out var reason))
            {
                ReadStateOfTheReadModel(namedReadModel, values, source!, draft);
            }
            else
            {
                draft.CannotRead(reason!);
            }
        }
        else if (draft.When is null)
        {
            draft.CannotRead("the command it issues is put together somewhere this cannot read");
        }
        else if (draft.Then.Count == 0 && draft.Errors.Count == 0)
        {
            draft.CannotRead("it expects no event and no rejection, and those are the outcomes the language holds");
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

        var specification = new SpecificationModel(name, [.. draft.Given], draft.When, [.. draft.Then], [.. draft.Errors]);
        SpecificationEvidence.Register(
            specification,
            new(
                type,
                type.Locations.First(location => location.IsInSource),
                draft.GetStateEvidence(),
                draft.GetValueEvidence(),
                draft.GetErrorEvidence(),
                [.. stated.All]));
        return specification;
    }

    /// <summary>
    /// Determines whether a type is written the way a specification is, whatever it turns out to specify.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is shaped like a specification.</returns>
    static bool IsWrittenAsOne(INamedTypeSymbol type)
    {
        if (type is not { TypeKind: TypeKind.Class, IsAbstract: false, ContainingType: null })
        {
            return false;
        }

        var steps = SpecificationMembers.StepsOf(type);
        return SpecificationMembers.MethodsIn(steps, SpecificationMembers.BecauseMethod).Any() ||
               (SpecificationMembers.ReadModelOf(steps) is not null &&
                SpecificationMembers.MethodsIn(steps, SpecificationMembers.EstablishMethod).Any());
    }

    /// <summary>
    /// States the read model a scenario is about as what followed the events it started from.
    /// </summary>
    /// <param name="readModel">The read model the scenario is of.</param>
    /// <param name="values">Every exact read-model value asserted by the generated scenario.</param>
    /// <param name="source">The exact read-model scenario type-argument source.</param>
    /// <param name="draft">The scenario collected so far.</param>
    /// <remarks>
    /// A scenario of a read model has no command to issue - the events are what happened and the model is what they
    /// built - so what it says is the two halves the language already holds: <c>given</c> the events, and then the
    /// <c>readmodel</c> they left behind. Generated assertions against the scenario instance state every resulting
    /// value exactly; an incomplete, repeated, conditional, or computed assertion blocks the whole scenario.
    /// <para>
    /// A scenario stating nothing that had happened is left out rather than written, because a read model with no
    /// events behind it is the empty one every read model starts as, and saying so describes nothing the application
    /// does.
    /// </para>
    /// </remarks>
    static void ReadStateOfTheReadModel(
        ITypeSymbol readModel,
        IReadOnlyList<PropertyMappingModel> values,
        Location source,
        SpecificationDraft draft)
    {
        if (draft.Given.Count == 0)
        {
            draft.CannotRead("it states nothing that had happened, and what a read model holds is what happened before it");
            return;
        }

        var state = new SpecificationStateModel(readModel.Name, SpecificationStateKind.ReadModel, values);
        draft.AddThen(state, readModel, source);
    }

    /// <summary>
    /// Determines whether the steps of a specification drive a slice rather than exercise a part of one.
    /// </summary>
    /// <param name="steps">The type the steps are written on.</param>
    /// <returns>True when the specification issues a command, or says what the event store already held.</returns>
    /// <remarks>
    /// A specification against a running host holds no scenario of its own - the host is the scenario - so what makes
    /// it one is that it hands a command to the host, or says what the event log already held before doing so.
    /// <para>
    /// Where the two are looked for differs, and deliberately. Issuing a command is what a slice does, wherever it is
    /// written. Appending to a sequence is only the world a scenario starts in when it happens before the action, and
    /// a specification appending as its action is one about the event store itself - a constraint holding, an append
    /// being rejected - which is a part of a slice rather than the behavior of one.
    /// </para>
    /// </remarks>
    bool DrivesASlice(INamedTypeSymbol steps) =>
        Calls(steps, SpecificationMembers.EstablishMethod)
            .Any(_ => SpecificationCalls.IsGivenEvents(_) || SpecificationCalls.IsGivenReadModel(_) || SpecificationCalls.IsExecution(_)) ||
        Calls(steps, SpecificationMembers.BecauseMethod).Any(SpecificationCalls.IsExecution);

    /// <summary>
    /// Gets every call a method of a chain resolves to.
    /// </summary>
    /// <param name="steps">The type the steps are written on.</param>
    /// <param name="method">The name of the method to read.</param>
    /// <returns>The methods called.</returns>
    IEnumerable<IMethodSymbol> Calls(INamedTypeSymbol steps, string method) =>
        SpecificationMembers.MethodsIn(steps, method).SelectMany(HandlerBodies.Of).SelectMany(CallsIn);

    /// <summary>
    /// Gets every call a body resolves to.
    /// </summary>
    /// <param name="body">The body to walk.</param>
    /// <returns>The methods called, empty when the project the body was written in was not handed over.</returns>
    IEnumerable<IMethodSymbol> CallsIn(SyntaxNode body)
    {
        if (models.For(body.SyntaxTree) is not { } semanticModel)
        {
            return [];
        }

        return body.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => semanticModel.GetSymbolInfo(invocation).Symbol)
            .OfType<IMethodSymbol>();
    }
}
