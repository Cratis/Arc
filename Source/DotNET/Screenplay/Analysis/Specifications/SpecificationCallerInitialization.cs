// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Rejects initialization that can freeze the scenario's provider before caller registration.
/// </summary>
internal static class SpecificationCallerInitialization
{
    /// <summary>
    /// Checks all scenario-chain calls, including constructors, initializers, and helpers.
    /// </summary>
    /// <param name="calls">The calls in the scenario chain.</param>
    /// <param name="action">The single command action.</param>
    /// <param name="registration">The supported caller registration.</param>
    /// <returns>The failure reason, or null when initialization cannot precede registration.</returns>
    internal static string? Check(IReadOnlyList<SpecificationCallerCall> calls, SpecificationCallerCall action, SpecificationCallerCall registration) =>
        calls.Any(call => CanInitializeBeforeRegistration(call, action, registration))
            ? "the command scenario may already be initialized before the caller is registered" : null;

    /// <summary>
    /// Determines whether another call can initialize the action's scenario too early. Any command-scenario initialization
    /// counts, whatever reference it goes through, because an alias or a helper parameter can name the same scenario.
    /// </summary>
    /// <param name="call">The possible initializing call.</param>
    /// <param name="action">The single command action.</param>
    /// <param name="registration">The supported caller registration.</param>
    /// <returns>Whether initialization is not proven to follow registration.</returns>
    static bool CanInitializeBeforeRegistration(SpecificationCallerCall call, SpecificationCallerCall action, SpecificationCallerCall registration)
    {
        if (!IsInitializing(call.Method))
        {
            return false;
        }

        if (call == action)
        {
            return false;
        }

        return !FollowsRegistration(call, registration);
    }

    /// <summary>
    /// Recognizes the scenario methods that build its provider on first use.
    /// </summary>
    /// <param name="method">The resolved method.</param>
    /// <returns>Whether the method initializes a command scenario.</returns>
    static bool IsInitializing(IMethodSymbol? method) =>
        method is { } initializing && initializing.ContainingType.Is(WellKnownTypeNames.CommandScenario) && IsInitializingName(initializing.Name);

    /// <summary>
    /// Recognizes the two command-scenario initialization entry points.
    /// </summary>
    /// <param name="name">The method name.</param>
    /// <returns>Whether the method executes or validates a command.</returns>
    static bool IsInitializingName(string name) => name == "Execute" || name == "Validate";

    /// <summary>
    /// Proves that a call runs after registration in the same unconditional Establish body.
    /// </summary>
    /// <param name="call">The initializing call.</param>
    /// <param name="registration">The caller registration.</param>
    /// <returns>Whether the call is proven to follow the registration.</returns>
    static bool FollowsRegistration(SpecificationCallerCall call, SpecificationCallerCall registration)
    {
        if (!call.InEstablish)
        {
            return false;
        }

        if (!call.Unconditional)
        {
            return false;
        }

        return SameEstablish(call, registration) && call.Syntax.SpanStart > registration.Syntax.SpanStart;
    }

    /// <summary>
    /// Compares the actual Establish method symbols rather than their text names.
    /// </summary>
    /// <param name="call">The initializing call.</param>
    /// <param name="registration">The caller registration.</param>
    /// <returns>Whether both calls belong to the same method.</returns>
    static bool SameEstablish(SpecificationCallerCall call, SpecificationCallerCall registration) =>
        SymbolEqualityComparer.Default.Equals(EnclosingMethod(call), EnclosingMethod(registration));

    /// <summary>
    /// Resolves the source method containing one call.
    /// </summary>
    /// <param name="call">The source call.</param>
    /// <returns>The enclosing method symbol, or null when the call is not in a method.</returns>
    static ISymbol? EnclosingMethod(SpecificationCallerCall call) =>
        call.Syntax.FirstAncestorOrSelf<MethodDeclarationSyntax>() is { } method ? call.Model.GetDeclaredSymbol(method) : null;
}
