// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Validation;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Policies;

/// <summary>
/// Reads what a registered policy requires of the caller, from the builder its registration configures.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unrecoverable is reported to.</param>
/// <remarks>
/// Requirements declared one after the other all have to hold, so they combine with <c>and</c>. The several values
/// one requirement accepts are alternatives, so they combine with <c>or</c>. A requirement that only code can decide
/// is reported and left out, which states less than the truth rather than something other than it.
/// </remarks>
public class PolicyRequirementReader(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// The call requiring nothing of the caller beyond being authenticated.
    /// </summary>
    public const string RequireAuthenticatedUser = "RequireAuthenticatedUser";

    /// <summary>
    /// The call requiring the caller to hold one of a set of roles.
    /// </summary>
    public const string RequireRole = "RequireRole";

    /// <summary>
    /// The call requiring the caller to carry a claim.
    /// </summary>
    public const string RequireClaim = "RequireClaim";

    /// <summary>
    /// Reads what a policy requires.
    /// </summary>
    /// <param name="registration">The registration to read.</param>
    /// <returns>The requirement, or <see langword="null"/> when nothing could be recovered.</returns>
    public PolicyRequirementModel? Read(PolicyRegistration registration)
    {
        if (registration.Configure is not AnonymousFunctionExpressionSyntax configure)
        {
            Report(registration, "it is registered from a policy built in code rather than configured inline");

            return null;
        }

        var calls = Calls(configure.Body, registration.SemanticModel).ToList();
        var steps = calls.Select(_ => Step(_, registration)).OfType<PolicyRequirementModel>().ToList();

        if (calls.Count == 0)
        {
            Report(registration, "it declares no requirement at all");
        }

        return steps.Count == 0 ? null : steps.Aggregate((left, right) => new CombinedRequirement(left, false, right));
    }

    /// <summary>
    /// Gets the requirements a configuration declares, in the order they were written.
    /// </summary>
    /// <param name="body">The body of the configuration.</param>
    /// <param name="semanticModel">The semantic model of the tree the configuration lives in.</param>
    /// <returns>The calls, ordered by where the name of each one was written.</returns>
    /// <remarks>
    /// A chain is nested outermost first in the tree while a block is written top to bottom, so both are ordered by
    /// where each call names itself. That is the order the developer reads, and the order the document has to keep.
    /// </remarks>
    static IEnumerable<InvocationExpressionSyntax> Calls(SyntaxNode body, SemanticModel semanticModel) =>
        body.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Where(_ => semanticModel.GetSymbolInfo(_).Symbol is IMethodSymbol method &&
                        method.ContainingType.Is(WellKnownTypeNames.AuthorizationPolicyBuilder))
            .OrderBy(_ => (_.Expression as MemberAccessExpressionSyntax)?.Name.SpanStart ?? _.SpanStart);

    /// <summary>
    /// Combines a set of alternatives into one requirement.
    /// </summary>
    /// <param name="alternatives">The alternatives to combine, of which there is at least one.</param>
    /// <returns>The requirement.</returns>
    static PolicyRequirementModel AnyOf(IEnumerable<PolicyRequirementModel> alternatives) =>
        alternatives.Aggregate((left, right) => new CombinedRequirement(left, true, right));

    /// <summary>
    /// Reads one requirement.
    /// </summary>
    /// <param name="call">The call declaring it.</param>
    /// <param name="registration">The registration the call belongs to.</param>
    /// <returns>The requirement, or <see langword="null"/> when it has no counterpart.</returns>
    PolicyRequirementModel? Step(InvocationExpressionSyntax call, PolicyRegistration registration)
    {
        var name = InvocationChain.NameOf(call);
        var arguments = call.ArgumentList.Arguments.Select(_ => _.Expression).ToList();

        if (string.Equals(name, RequireAuthenticatedUser, StringComparison.Ordinal))
        {
            return AuthenticatedRequirement.Instance;
        }

        if (string.Equals(name, RequireRole, StringComparison.Ordinal) &&
            PolicyValues.Of(arguments, registration.SemanticModel) is { Count: > 0 } roles)
        {
            return AnyOf(roles.Select(_ => new RoleRequirement(_)));
        }

        if (string.Equals(name, RequireClaim, StringComparison.Ordinal) &&
            arguments.Count > 1 &&
            PolicyValues.Of(arguments, registration.SemanticModel) is { Count: > 1 } claim)
        {
            return AnyOf(claim.Skip(1).Select(_ => new ClaimRequirement(claim[0], _)));
        }

        Report(registration, $"'{name}' has no counterpart in a policy condition");

        return null;
    }

    /// <summary>
    /// Reports what could not be recovered about a policy.
    /// </summary>
    /// <param name="registration">The registration being read.</param>
    /// <param name="reason">Why it could not be recovered.</param>
    void Report(PolicyRegistration registration, string reason) =>
        diagnostics.Warning(
            ScreenplayDiagnosticCodes.PolicyRequirementsUnrecoverable,
            $"The policy '{registration.Name}' is referred to, but {reason}, so that part of what it requires is not stated",
            registration.Location);
}
