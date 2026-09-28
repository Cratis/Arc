// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Types;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Evaluates authentication and roles declared on a type or method. Policy-bearing declarations require the asynchronous pipeline.
/// </summary>
/// <param name="currentPrincipalAccessor">The current principal.</param>
/// <param name="anonymousEvaluators">The anonymous attribute evaluators.</param>
/// <param name="authorizationAttributeEvaluators">The authorization attribute evaluators.</param>
public class AuthorizationEvaluator(
    ICurrentPrincipalAccessor currentPrincipalAccessor,
    IInstancesOf<IAnonymousEvaluator> anonymousEvaluators,
    IInstancesOf<IAuthorizationAttributeEvaluator> authorizationAttributeEvaluators) : IAuthorizationEvaluator
{
    static readonly AsyncLocal<AuthorizedEvaluation?> _alreadyEvaluated = new();
    readonly AuthorizationDeclarations _declarations = new(anonymousEvaluators, authorizationAttributeEvaluators);

    /// <summary>
    /// Initializes the evaluator with baseline requirements discovered by convention.
    /// </summary>
    /// <param name="currentPrincipalAccessor">The current principal.</param>
    /// <param name="anonymousEvaluators">The anonymous attribute evaluators.</param>
    /// <param name="authorizationAttributeEvaluators">The authorization attribute evaluators.</param>
    /// <param name="fallbackEvaluators">Baseline requirements used only when no explicit declaration applies.</param>
    public AuthorizationEvaluator(
        ICurrentPrincipalAccessor currentPrincipalAccessor,
        IInstancesOf<IAnonymousEvaluator> anonymousEvaluators,
        IInstancesOf<IAuthorizationAttributeEvaluator> authorizationAttributeEvaluators,
        IInstancesOf<IFallbackAuthorizationEvaluator> fallbackEvaluators)
        : this(currentPrincipalAccessor, anonymousEvaluators, authorizationAttributeEvaluators) =>
        _declarations = new(anonymousEvaluators, authorizationAttributeEvaluators, fallbackEvaluators);

    /// <summary>
    /// Checks the authentication and roles of an already selected principal.
    /// </summary>
    /// <param name="declaration">The effective declaration.</param>
    /// <param name="principal">The selected principal.</param>
    /// <returns>True if the caller meets the authentication and role requirements.</returns>
    /// <exception cref="AsynchronousAuthorizationRequired">A policy or scheme requires the asynchronous pipeline.</exception>
    public static bool Check(AuthorizationDeclaration declaration, ClaimsPrincipal? principal)
    {
        if (declaration.AllowsAnonymous || declaration.Requirements.Count == 0)
        {
            return true;
        }

        if (declaration.RequiresAsynchronousEvaluation)
        {
            throw new AsynchronousAuthorizationRequired();
        }

        return CheckRoles(declaration, principal);
    }

    /// <inheritdoc/>
    public bool IsAuthorized(Type type) => CheckMember(type, _declarations.For(type));

    /// <inheritdoc/>
    public bool IsAuthorized(MethodInfo method) => CheckMember(method, _declarations.For(method));

    /// <summary>
    /// Checks authentication and roles after the asynchronous requirements have been evaluated.
    /// </summary>
    /// <param name="declaration">The effective declaration.</param>
    /// <param name="principal">The selected principal.</param>
    /// <param name="evaluatesAnonymous">Whether all resolved policies allow anonymous evaluation.</param>
    /// <returns>True when all authentication and role requirements are met.</returns>
    internal static bool CheckRoles(AuthorizationDeclaration declaration, ClaimsPrincipal? principal, bool evaluatesAnonymous = false) =>
        declaration.Requirements.Count == 0 ||
        ((principal?.Identity?.IsAuthenticated == true || evaluatesAnonymous) &&
         declaration.Requirements.All(requirement => requirement.AnyOfRoles.Count == 0 ||
             (principal?.Identity?.IsAuthenticated == true && requirement.AnyOfRoles.Any(principal.IsInRole))));

    /// <summary>
    /// Detects authentication on any identity, including identities added after an unauthenticated primary identity.
    /// </summary>
    /// <param name="principal">The principal to inspect.</param>
    /// <returns>True if any identity is authenticated.</returns>
    internal static bool HasAuthenticatedIdentity(ClaimsPrincipal? principal) => principal?.Identities.Any(identity => identity.IsAuthenticated) == true;

    /// <summary>
    /// Allows a legacy evaluator to delegate to the default evaluator only for requirements already checked on this target and principal.
    /// </summary>
    /// <param name="target">The evaluated command type or query method.</param>
    /// <param name="principal">The selected principal.</param>
    /// <param name="declaration">The exact effective requirements already checked.</param>
    /// <param name="evaluatesAnonymous">Whether anonymous evaluation was explicitly opted in.</param>
    /// <returns>A scope removing the permission immediately after the legacy verdict.</returns>
    internal static IDisposable AlreadyEvaluated(MemberInfo target, ClaimsPrincipal? principal, AuthorizationDeclaration declaration, bool evaluatesAnonymous = false) =>
        AlreadyEvaluated(target, AuthorizationPrincipalIdentity.Capture(principal), declaration, evaluatesAnonymous);

    /// <summary>
    /// Marks an asynchronously evaluated execution identity using its snapshot from immediately before policy execution.
    /// </summary>
    /// <param name="target">The evaluated command type or query method.</param>
    /// <param name="principal">The execution identity captured immediately before policy evaluation.</param>
    /// <param name="declaration">The exact effective requirements already checked.</param>
    /// <param name="evaluatesAnonymous">Whether anonymous evaluation was explicitly opted in.</param>
    /// <returns>A scope removing the permission immediately after the legacy verdict.</returns>
    internal static IDisposable AlreadyEvaluated(MemberInfo target, PrincipalSnapshot principal, AuthorizationDeclaration declaration, bool evaluatesAnonymous = false)
    {
        var previous = _alreadyEvaluated.Value;
        _alreadyEvaluated.Value = new AuthorizedEvaluation(target, principal, declaration, evaluatesAnonymous);
        return new EvaluationScope(previous);
    }

    /// <summary>
    /// Compares effective requirements by content so another declaration cannot reuse an authorization verdict.
    /// </summary>
    /// <remarks>
    /// Roles-only requirements can be reordered and repeated roles do not change their verdict; repeated requirements
    /// must still match one for one. The ordered policy and explicit scheme sequence across requirements is significant:
    /// policy-resolved schemes can determine which authenticated identity becomes primary.
    /// </remarks>
    /// <param name="checkedDeclaration">The already evaluated requirements.</param>
    /// <param name="current">The requirements being checked now.</param>
    /// <returns>Whether they describe the same effective authorization.</returns>
    internal static bool SameDeclaration(AuthorizationDeclaration checkedDeclaration, AuthorizationDeclaration current)
    {
        if (checkedDeclaration.AllowsAnonymous != current.AllowsAnonymous ||
            checkedDeclaration.IsExplicit != current.IsExplicit ||
            checkedDeclaration.Requirements.Count != current.Requirements.Count)
        {
            return false;
        }

        if (!OrderedSchemesAndPolicies(checkedDeclaration).SequenceEqual(OrderedSchemesAndPolicies(current)))
        {
            return false;
        }

        var unmatched = current.Requirements.ToList();
        foreach (var requirement in checkedDeclaration.Requirements)
        {
            var match = unmatched.FindIndex(candidate => SameRequirement(requirement, candidate));
            if (match < 0)
            {
                return false;
            }

            unmatched.RemoveAt(match);
        }

        return true;
    }

    static IEnumerable<(bool IsPolicy, string Name)> OrderedSchemesAndPolicies(AuthorizationDeclaration declaration)
    {
        foreach (var requirement in declaration.Requirements)
        {
            foreach (var scheme in requirement.AuthenticationSchemes)
            {
                yield return (false, scheme);
            }

            if (requirement.Policy is { } policy)
            {
                yield return (true, policy);
            }
        }
    }

    static bool SameRequirement(AuthorizationRequirement checkedRequirement, AuthorizationRequirement current) =>
        string.Equals(checkedRequirement.Policy, current.Policy, StringComparison.Ordinal) &&
        new HashSet<string>(checkedRequirement.AnyOfRoles, StringComparer.Ordinal).SetEquals(current.AnyOfRoles) &&
        checkedRequirement.AuthenticationSchemes.SequenceEqual(current.AuthenticationSchemes, StringComparer.Ordinal);

    bool CheckMember(MemberInfo target, AuthorizationDeclaration declaration)
    {
        var principal = currentPrincipalAccessor.Current;
        if (declaration.RequiresAsynchronousEvaluation &&
            _alreadyEvaluated.Value is { } checkedEvaluation &&
            checkedEvaluation.Target.Equals(target) && AuthorizationPrincipalIdentity.Same(checkedEvaluation.Principal, principal) &&
            (!checkedEvaluation.EvaluatesAnonymous || !HasAuthenticatedIdentity(principal)) &&
            SameDeclaration(checkedEvaluation.Declaration, declaration))
        {
            return CheckRoles(declaration, principal, checkedEvaluation.EvaluatesAnonymous);
        }

        return Check(declaration, principal);
    }

    sealed record AuthorizedEvaluation(MemberInfo Target, PrincipalSnapshot Principal, AuthorizationDeclaration Declaration, bool EvaluatesAnonymous);

    sealed class EvaluationScope(AuthorizedEvaluation? previous) : IDisposable
    {
        public void Dispose() => _alreadyEvaluated.Value = previous;
    }
}
