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
/// <param name="compilation">The compilation being analyzed.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unreadable is reported to.</param>
/// <remarks>
/// Only a specification driving a command through the real pipeline is read. A unit level one stands a collaborator
/// up behind a substitute and says what that collaborator was asked to do, which is a statement about the inside of
/// the slice rather than about its behavior, and Screenplay has nowhere to put it. Whether a specification is one or
/// the other is decided by what it touches: holding a scenario the pipeline runs in, or reaching the event log, is
/// what an integration specification does and nothing else does.
/// </remarks>
public class SpecificationReader(Compilation compilation, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Determines whether a type specifies a slice by driving a command through the pipeline.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is a specification of the kind this reads.</returns>
    public bool IsSpecification(INamedTypeSymbol type)
    {
        if (type is not { TypeKind: TypeKind.Class, IsAbstract: false, ContainingType: null })
        {
            return false;
        }

        var steps = SpecificationMembers.StepsOf(type);

        return SpecificationMembers.MethodsIn(steps, SpecificationMembers.BecauseMethod).Any() &&
            (SpecificationMembers.HoldsAScenario(steps) || DrivesASlice(steps));
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
        var draft = new SpecificationDraft();
        var stated = new ScreenplayDiagnostics();

        var reader = new SpecificationStepReader(compilation, new(stated));
        reader.ReadGiven(steps, draft, name, location);
        reader.ReadWhen(steps, draft, name, location);
        new SpecificationOutcomeReader(compilation, stated).Read(type, draft, name, location);

        if (draft.When is null)
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

        return new(name, [.. draft.Given], draft.When, [.. draft.Then], [.. draft.Errors]);
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
    /// <returns>The methods called.</returns>
    IEnumerable<IMethodSymbol> CallsIn(SyntaxNode body)
    {
        var semanticModel = compilation.GetSemanticModel(body.SyntaxTree);

        return body.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => semanticModel.GetSymbolInfo(invocation).Symbol)
            .OfType<IMethodSymbol>();
    }
}
