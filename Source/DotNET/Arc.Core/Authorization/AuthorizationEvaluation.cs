// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Applies asynchronous policy and scheme requirements without replacing legacy evaluator verdicts.
/// </summary>
/// <param name="declarations">The effective declarations.</param>
/// <param name="evaluator">The configured legacy evaluator.</param>
/// <param name="principalAccessor">The current principal.</param>
/// <param name="runtime">The host's policy and authentication runtime.</param>
public class AuthorizationEvaluation(
    AuthorizationDeclarations declarations,
    IAuthorizationEvaluator evaluator,
    ICurrentPrincipalAccessor principalAccessor,
    IAuthorizationPolicyRuntime runtime)
{
    /// <summary>
    /// Authorizes a command or query against its declaration.
    /// </summary>
    /// <param name="target">The command type or query method.</param>
    /// <param name="resource">The command or query context.</param>
    /// <param name="services">The executing scope.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <param name="legacyVerdict">An optional query performer's legacy authorization verdict.</param>
    /// <returns>True only when all declared requirements and legacy checks allow access.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">The target or configuration is unsupported.</exception>
    public Task<bool> IsAuthorized(MemberInfo target, object resource, IServiceProvider services, CancellationToken cancellationToken, Func<bool>? legacyVerdict = null) =>
        IsAuthorized(target, resource, services, legacyVerdict, evaluator, cancellationToken);

    /// <summary>
    /// Resolves schemes before identity-bound command values and execution scopes are created, but does not evaluate resource policies yet.
    /// </summary>
    /// <param name="target">The protected member.</param>
    /// <param name="services">The executing scope.</param>
    /// <param name="cancellationToken">The execution cancellation token.</param>
    /// <returns>The stable authorization plan for the later filter verdict.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">The declaration or policy is invalid.</exception>
    internal async Task<PreparedAuthorization> Prepare(MemberInfo target, IServiceProvider services, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var declaration = target switch
        {
            Type type => declarations.For(type),
            MethodInfo method => declarations.For(method),
            _ => throw new InvalidAuthorizationConfiguration($"Unsupported authorization target '{target}'.")
        };
        var resolution = await runtime.Resolve(declaration.Requirements, services, cancellationToken);
        var originalPrincipal = principalAccessor.Current;
        var selectedPrincipal = await resolution.SelectPrincipal(originalPrincipal, services, cancellationToken);
        if (resolution is IAnonymousPolicyResolution { EvaluatesAnonymous: true } &&
            selectedPrincipal?.Identity?.IsAuthenticated != true)
        {
            selectedPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new PreparedAuthorization(target, declaration, originalPrincipal, selectedPrincipal, resolution);
    }

    /// <summary>
    /// Uses the evaluator supplied to an existing filter rather than replacing it with the DI evaluator.
    /// </summary>
    /// <param name="target">The protected member.</param>
    /// <param name="resource">The command or query context.</param>
    /// <param name="services">The executing scope.</param>
    /// <param name="legacyVerdict">An optional performer's legacy verdict.</param>
    /// <param name="legacyEvaluator">The evaluator originally passed to the filter.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The combined authorization verdict.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">The target or configuration is unsupported.</exception>
    internal async Task<bool> IsAuthorized(MemberInfo target, object resource, IServiceProvider services, Func<bool>? legacyVerdict, IAuthorizationEvaluator legacyEvaluator, CancellationToken cancellationToken)
    {
        var prepared = resource switch
        {
            CommandContext { PreparedAuthorization: { } commandPlan } => commandPlan,
            QueryContext { PreparedAuthorization: { } queryPlan } => queryPlan,
            _ => await Prepare(target, services, cancellationToken)
        };
        if (!prepared.Target.Equals(target))
        {
            throw new InvalidAuthorizationConfiguration("The prepared authorization target does not match the executing command.");
        }

        var currentDeclaration = target switch
        {
            Type type => declarations.For(type),
            MethodInfo method => declarations.For(method),
            _ => throw new InvalidAuthorizationConfiguration($"Unsupported authorization target '{target}'.")
        };
        var declaration = prepared.Declaration;
        if (!AuthorizationEvaluator.SameDeclaration(declaration, currentDeclaration))
        {
            throw new InvalidAuthorizationConfiguration("Authorization requirements changed during execution.");
        }

        var selectedPrincipal = prepared.SelectedPrincipal;
        var resolution = prepared.Resolution;
        cancellationToken.ThrowIfCancellationRequested();
        if (!AuthorizationEvaluator.CheckRoles(declaration, selectedPrincipal, prepared.EvaluatesAnonymous))
        {
            return false;
        }

        var principalChanged = prepared.PrincipalChanged;
        var needsSelectedScope = principalChanged && !ReferenceEquals(principalAccessor.Current, selectedPrincipal);
        using var selectedScope = needsSelectedScope
            ? services.GetRequiredService<AuthorizationPrincipalScope>().Begin(selectedPrincipal!, services)
            : null;

        if (declaration.RequiresAsynchronousEvaluation)
        {
            if (selectedPrincipal is null || !await resolution.IsAuthorized(
                new AuthorizationPolicyContext(selectedPrincipal, target, resource),
                services,
                cancellationToken))
            {
                return false;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Only the same target and selected principal may be checked synchronously after its asynchronous requirements.
        using var alreadyEvaluated = declaration.RequiresAsynchronousEvaluation && selectedPrincipal is not null
            ? AuthorizationEvaluator.AlreadyEvaluated(target, principalAccessor.Current, declaration, prepared.EvaluatesAnonymous)
            : null;

        // A performer's captured verdict replaces the dispatcher fallback; neither can bypass declared requirements.
        var allowed = legacyVerdict?.Invoke() ?? target switch
        {
            Type type => legacyEvaluator.IsAuthorized(type),
            MethodInfo method => legacyEvaluator.IsAuthorized(method),
            _ => throw new InvalidAuthorizationConfiguration($"Unsupported authorization target '{target}'.")
        };
        if (!allowed)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var identity = principalChanged
            ? new ClaimsPrincipal(selectedPrincipal!.Identities.Select(claimsIdentity => claimsIdentity.Clone()))
            : null;
        if (resource is QueryContext queryContext)
        {
            queryContext.AuthorizedPrincipal = identity;
        }
        else if (resource is CommandContext commandContext)
        {
            commandContext.AuthorizedPrincipal = identity;
        }

        return true;
    }
}
