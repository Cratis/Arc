// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Recovers an exact caller only from one unconditional documented setup.
/// </summary>
/// <param name="models">The application semantic models.</param>
/// <param name="held">The held-value stability checker.</param>
internal sealed class SpecificationCallerReader(SemanticModels models, HeldValues held)
{
    /// <summary>
    /// Checks the supported accessor setup.
    /// </summary>
    readonly SpecificationCallerSetup _setup = new(models, held);

    /// <summary>
    /// Checks that nothing else uses the scenario's service collection.
    /// </summary>
    readonly SpecificationCallerServices _services = new(models);

    /// <summary>
    /// Reads the specification's caller setup without guessing hidden host state.
    /// </summary>
    /// <param name="type">The type containing the scenario steps.</param>
    /// <returns>The caller or a reason it cannot be represented.</returns>
    internal SpecificationCallerRecovery Read(INamedTypeSymbol type)
    {
        var calls = _setup.CallsIn(type);
        var factories = calls.Where(SpecificationCallerCalls.IsFactory).ToList();
        var configurations = calls.Where(SpecificationCallerCalls.IsConfiguration).ToList();
        var uses = calls.Where(SpecificationCallerCalls.IsAuthorizationUse).ToList();
        if (factories.Count + configurations.Count + uses.Count == 0)
        {
            return new(null, null);
        }

        if (factories.Count != 1)
        {
            return SpecificationCallerRecovery.Failed("the caller needs exactly one accessor construction and registration");
        }

        if (configurations.Count != 1)
        {
            return SpecificationCallerRecovery.Failed("the caller needs exactly one Current.Returns configuration");
        }

        var factory = factories[0];
        var configuration = configurations[0];
        var registrations = uses.Where(call => call.Method?.Name == "AddSingleton").ToList();
        if (registrations.Count != 1)
        {
            return SpecificationCallerRecovery.Failed("the caller needs exactly one principal registration, without other authorization service registrations");
        }

        return ReadSetup(type, calls, new(factory, configuration, registrations[0]));
    }

    /// <summary>
    /// Recognizes the command execution in Because.
    /// </summary>
    /// <param name="call">The source call.</param>
    /// <returns>Whether the call is the command action.</returns>
    static bool IsCommandAction(SpecificationCallerCall call) =>
        call.Method?.Name == "Execute" && IsBecauseOnCommandScenario(call);

    /// <summary>
    /// Determines whether the call is on a command scenario in Because.
    /// </summary>
    /// <param name="call">The source call.</param>
    /// <returns>Whether the call has the supported action location.</returns>
    static bool IsBecauseOnCommandScenario(SpecificationCallerCall call) =>
        call.Method!.ContainingType.Is(WellKnownTypeNames.CommandScenario) &&
        call.Syntax.FirstAncestorOrSelf<MethodDeclarationSyntax>()?.Identifier.ValueText == SpecificationMembers.BecauseMethod;

    /// <summary>
    /// Determines whether both setup calls always execute in Establish.
    /// </summary>
    /// <param name="setup">The recovered setup.</param>
    /// <returns>Whether configuration and registration are unconditional.</returns>
    static bool IsAlwaysSetup(Setup setup) => IsAlwaysEstablish(setup.Configuration) && IsAlwaysEstablish(setup.Registration);

    /// <summary>
    /// Determines whether one call always executes in Establish.
    /// </summary>
    /// <param name="call">The source call.</param>
    /// <returns>Whether the call is unconditional setup.</returns>
    static bool IsAlwaysEstablish(SpecificationCallerCall call) => call.InEstablish && call.Unconditional;

    /// <summary>
    /// Checks that construction and configuration precede registration.
    /// </summary>
    /// <param name="setup">The supported setup calls.</param>
    /// <param name="calls">All calls in source-chain order.</param>
    /// <returns>Whether the setup has the supported order.</returns>
    static bool Ordered(Setup setup, IReadOnlyList<SpecificationCallerCall> calls)
    {
        var nodes = calls.ToList();
        if (nodes.IndexOf(setup.Configuration) > nodes.IndexOf(setup.Registration))
        {
            return false;
        }

        return setup.Factory.Syntax.FirstAncestorOrSelf<MethodDeclarationSyntax>() is null ||
            nodes.IndexOf(setup.Factory) < nodes.IndexOf(setup.Configuration);
    }

    /// <summary>
    /// Validates setup and initialization before reading the principal literal.
    /// </summary>
    /// <param name="type">The specification type.</param>
    /// <param name="calls">The calls throughout its source chain.</param>
    /// <param name="setup">The three supported setup calls.</param>
    /// <returns>The recovered caller or an exactness failure.</returns>
    SpecificationCallerRecovery ReadSetup(INamedTypeSymbol type, IReadOnlyList<SpecificationCallerCall> calls, Setup setup)
    {
        var actions = calls.Where(IsCommandAction).ToList();
        if (actions.Count != 1)
        {
            return SpecificationCallerRecovery.Failed("the caller is not attached to one in-process command scenario action");
        }

        var allowed = new[] { setup.Factory, setup.Configuration, setup.Registration };
        if (calls.Where(SpecificationCallerCalls.IsAuthorizationUse).Except(allowed).Any())
        {
            return SpecificationCallerRecovery.Failed("the caller uses another authorization service or system execution");
        }

        if (!IsAlwaysSetup(setup))
        {
            return SpecificationCallerRecovery.Failed("the caller setup is conditional, repeated, deferred, or outside Establish");
        }

        var reason = SpecificationCallerInitialization.Check(calls, actions[0], setup.Registration);
        reason ??= SetupFailure(type, setup, actions[0]);
        reason ??= Ordered(setup, calls) ? null : "the caller configuration must precede registration and execution";
        if (reason is not null)
        {
            return SpecificationCallerRecovery.Failed(reason);
        }

        return SpecificationPrincipalReader.Read(setup.Configuration.Syntax.ArgumentList.Arguments[0].Expression, setup.Configuration.Model);
    }

    /// <summary>
    /// Finds the first setup, accessor, or service-collection exactness failure.
    /// </summary>
    /// <param name="type">The specification type.</param>
    /// <param name="setup">The supported setup calls.</param>
    /// <param name="action">The command action.</param>
    /// <returns>The failure reason, or null for exact setup.</returns>
    string? SetupFailure(INamedTypeSymbol type, Setup setup, SpecificationCallerCall action)
    {
        var accessor = _setup.AssignedSymbol(setup.Factory);
        if (accessor is null)
        {
            return "the caller accessor is not held";
        }

        return _setup.CheckFactory(setup.Factory) ??
            _setup.CheckConfiguration(setup.Configuration, accessor) ??
            _setup.CheckRegistration(setup.Registration, action, accessor) ??
            OtherUse(type, accessor, [setup.Factory, setup.Configuration, setup.Registration]) ??
            _services.Check(type, setup.Registration);
    }

    /// <summary>
    /// Reports accessor uses outside the proven setup.
    /// </summary>
    /// <param name="type">The specification type.</param>
    /// <param name="accessor">The substituted accessor symbol.</param>
    /// <param name="allowed">The three proven setup calls.</param>
    /// <returns>The failure reason, or null when there are no other uses.</returns>
    string? OtherUse(INamedTypeSymbol type, ISymbol accessor, IReadOnlyList<SpecificationCallerCall> allowed) =>
        _setup.HasOtherAccessorUse(type, accessor, allowed) ? "the caller accessor is used elsewhere and may be mutated or reconfigured" : null;

    /// <summary>
    /// Holds the three source calls defining one caller setup.
    /// </summary>
    /// <param name="Factory">The accessor construction.</param>
    /// <param name="Configuration">The principal configuration.</param>
    /// <param name="Registration">The instance registration.</param>
    sealed record Setup(SpecificationCallerCall Factory, SpecificationCallerCall Configuration, SpecificationCallerCall Registration);
}
