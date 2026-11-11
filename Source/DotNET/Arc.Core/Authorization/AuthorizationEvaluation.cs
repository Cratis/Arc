// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
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
        var declaration = DeclarationFor(target);
        var resolution = await runtime.Resolve(declaration.Requirements, services, cancellationToken);
        var originalPrincipal = principalAccessor.Current;
        var selectedPrincipal = await resolution.SelectPrincipal(originalPrincipal, services, cancellationToken);
        selectedPrincipal = GuestWhenAnonymousEvaluated(resolution, selectedPrincipal);
        selectedPrincipal = AnonymousUnlessVerifiedCaller(services, selectedPrincipal);

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
        var request = new PreparedAuthorizationRequest(target, resource, services, legacyVerdict ?? (() => LegacyVerdict(target, legacyEvaluator)), prepared);
        return await IsAuthorizedPrepared(request, cancellationToken);
    }

    /// <summary>
    /// Evaluates a prepared authorization plan for a bound or explicitly resource-free operation.
    /// </summary>
    /// <param name="request">The <see cref="PreparedAuthorizationRequest"/> to evaluate.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The combined verdict.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">The prepared target or requirements changed.</exception>
    internal async Task<bool> IsAuthorizedPrepared(PreparedAuthorizationRequest request, CancellationToken cancellationToken)
    {
        var (target, _, services, _, prepared) = request;
        EnsurePlanIsCurrent(target, prepared);
        var selectedPrincipal = prepared.SelectedPrincipal;
        cancellationToken.ThrowIfCancellationRequested();
        if (!MeetsTypeRequirements(prepared))
        {
            return false;
        }

        var principalChanged = prepared.PrincipalChanged;
        var needsSelectedScope = principalChanged && !ReferenceEquals(principalAccessor.Current, selectedPrincipal);
        using var selectedScope = needsSelectedScope
            ? services.GetRequiredService<AuthorizationPrincipalScope>().Begin(selectedPrincipal!, services)
            : null;

        return await AuthorizeWithinScope(request, needsSelectedScope, cancellationToken);
    }

    /// <summary>
    /// Checks type requirements without evaluating policies that need a bound resource.
    /// </summary>
    /// <param name="check">The <see cref="TypeAuthorizationCheck"/> describing the unbound command type.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>Whether type requirements and policies explicitly safe without a resource allow access.</returns>
    internal async Task<bool> IsAuthorizedForType(TypeAuthorizationCheck check, CancellationToken cancellationToken)
    {
        var prepared = check.Prepared ?? await Prepare(check.Target, check.Services, cancellationToken);
        if (prepared.Resolution is IResourceFreePolicyResolution { CanEvaluateWithoutResource: true })
        {
            var request = new PreparedAuthorizationRequest(check.Target, null, check.Services, () => LegacyVerdict(check.Target, check.LegacyEvaluator), prepared);
            return await IsAuthorizedPrepared(request, cancellationToken);
        }

        if (!MeetsTypeRequirements(prepared))
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return prepared.Declaration.RequiresAsynchronousEvaluation || LegacyVerdict(check.Target, check.LegacyEvaluator);
    }

    static void PublishVerdict(PreparedAuthorizationRequest request, PolicyCertification? certification)
    {
        var (target, resource, _, _, prepared) = request;
        var authorizedExecution = certification is null ? null : new AuthorizedExecution(target, certification.Execution, prepared.Declaration, certification.Guest);
        PublishAuthorizedExecution(resource, prepared.PrincipalChanged ? prepared.SelectedPrincipal : null, authorizedExecution);
    }

    static IDisposable? AlreadyEvaluatedFor(PolicyCertification? certification, MemberInfo target, AuthorizationDeclaration declaration) =>
        certification is null ? null : AuthorizationEvaluator.AlreadyEvaluated(target, certification.Execution, declaration, certification.Guest);

    static ClaimsPrincipal? GuestWhenAnonymousEvaluated(IAuthorizationPolicyResolution resolution, ClaimsPrincipal? selectedPrincipal) =>
        resolution is IAnonymousPolicyResolution { EvaluatesAnonymous: true } && selectedPrincipal?.Identity?.IsAuthenticated != true
            ? new ClaimsPrincipal(new ClaimsIdentity())
            : selectedPrincipal;

    /// <summary>
    /// Refuses a principal a scheme selected other than the verified caller, or merged with another subject, by making it anonymous; never merges.
    /// </summary>
    /// <param name="services">The executing scope.</param>
    /// <param name="selectedPrincipal">The scheme-selected principal.</param>
    /// <returns>The selected principal, or an anonymous principal when it is not the verified caller.</returns>
    static ClaimsPrincipal? AnonymousUnlessVerifiedCaller(IServiceProvider services, ClaimsPrincipal? selectedPrincipal) =>
        services.GetService<IHttpRequestContextAccessor>()?.Current is { } request &&
        VerifiedCallerPrincipal.From(request) is { } verified &&
        verified.Identities.OfType<VerifiedCallerIdentity>().FirstOrDefault() is { } verifiedIdentity &&
        !CarriesOnlyVerifiedCaller(selectedPrincipal, verifiedIdentity)
            ? new ClaimsPrincipal(new ClaimsIdentity())
            : selectedPrincipal;

    static bool CarriesOnlyVerifiedCaller(ClaimsPrincipal? selectedPrincipal, VerifiedCallerIdentity verifiedIdentity) =>
        VerifiedCallerPrincipal.Carries(selectedPrincipal, verifiedIdentity.Caller) &&
        !VerifiedCallerPrincipal.CarriesAnotherSubject(selectedPrincipal, verifiedIdentity.Caller);

    static bool PolicyInputDiffers(bool exempt, PrincipalSnapshot policyIdentity, ClaimsPrincipal executionPrincipal) =>
        !exempt && !AuthorizationPrincipalIdentity.Same(policyIdentity, executionPrincipal);

    static bool GuestConflicts(bool guest, ClaimsPrincipal selectedPrincipal, ClaimsPrincipal executionPrincipal) =>
        guest && (AuthorizationEvaluator.HasAuthenticatedIdentity(selectedPrincipal) || AuthorizationEvaluator.HasAuthenticatedIdentity(executionPrincipal));

    static DateTimeOffset ReceivedAtFor(object? resource, IServiceProvider services) => resource switch
    {
        CommandContext commandContext when commandContext.ReceivedAt != default => commandContext.ReceivedAt,
        QueryContext queryReceiptContext when queryReceiptContext.ReceivedAt != default => queryReceiptContext.ReceivedAt,
        _ => OperationContextScope.Current ?? (services.GetService<TimeProvider>() ?? TimeProvider.System).GetUtcNow()
    };

    static void PublishAuthorizedExecution(object? resource, ClaimsPrincipal? selectedPrincipal, AuthorizedExecution? authorizedExecution)
    {
        var identity = selectedPrincipal is null
            ? null
            : new ClaimsPrincipal(selectedPrincipal.Identities.Select(claimsIdentity => claimsIdentity.Clone()));
        if (resource is QueryContext queryContext)
        {
            queryContext.AuthorizedPrincipal = identity;
            queryContext.AuthorizedExecution = authorizedExecution;
        }
        else if (resource is CommandContext commandContext)
        {
            commandContext.AuthorizedPrincipal = identity;
            commandContext.AuthorizedExecution = authorizedExecution;
        }
    }

    static bool MeetsTypeRequirements(PreparedAuthorization prepared) =>
        AuthorizationEvaluator.CheckRoles(prepared.Declaration, prepared.SelectedPrincipal, prepared.EvaluatesAnonymous);

    static bool LegacyVerdict(MemberInfo target, IAuthorizationEvaluator legacyEvaluator) => target switch
    {
        Type type => legacyEvaluator.IsAuthorized(type),
        MethodInfo method => legacyEvaluator.IsAuthorized(method),
        _ => throw new InvalidAuthorizationConfiguration($"Unsupported authorization target '{target}'.")
    };

    AuthorizationDeclaration DeclarationFor(MemberInfo target) => target switch
    {
        Type type => declarations.For(type),
        MethodInfo method => declarations.For(method),
        _ => throw new InvalidAuthorizationConfiguration($"Unsupported authorization target '{target}'.")
    };

    /// <summary>
    /// Checks that policy and legacy callbacks, which run application code, left the evaluated requirements in effect.
    /// </summary>
    /// <param name="target">The protected member.</param>
    /// <param name="declaration">The requirements the verdict evaluated.</param>
    /// <returns>Whether the verdict still certifies the current requirements.</returns>
    bool DeclarationUnchanged(MemberInfo target, AuthorizationDeclaration declaration) =>
        AuthorizationEvaluator.SameDeclaration(declaration, DeclarationFor(target));

    async Task<bool> AuthorizeWithinScope(PreparedAuthorizationRequest request, bool needsSelectedScope, CancellationToken cancellationToken)
    {
        var (target, _, _, legacyVerdict, prepared) = request;
        var declaration = prepared.Declaration;
        var certification = declaration.RequiresAsynchronousEvaluation
            ? await EvaluatePolicy(request, needsSelectedScope, cancellationToken)
            : null;
        if (declaration.RequiresAsynchronousEvaluation && certification is null)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // The legacy marker carries only the certified execution snapshot and guest flag; CheckMember rechecks both.
        using var alreadyEvaluated = AlreadyEvaluatedFor(certification, target, declaration);

        // A performer's captured verdict replaces the dispatcher fallback; neither can bypass declared requirements.
        if (!legacyVerdict())
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (certification is not null && !StillCertified(certification, target, declaration, prepared.SelectedPrincipal!))
        {
            return false;
        }

        PublishVerdict(request, certification);
        return true;
    }

    void EnsurePlanIsCurrent(MemberInfo target, PreparedAuthorization prepared)
    {
        if (!prepared.Target.Equals(target))
        {
            throw new InvalidAuthorizationConfiguration("The prepared authorization target does not match the executing command.");
        }

        if (!AuthorizationEvaluator.SameDeclaration(prepared.Declaration, DeclarationFor(target)))
        {
            throw new InvalidAuthorizationConfiguration("Authorization requirements changed during execution.");
        }
    }

    /// <summary>
    /// Evaluates the policy. A verdict certifies exactly the policy-input and execution identities captured immediately before
    /// evaluation (after pre-verdict hooks). Without a selected scope, authenticated policy input must
    /// match the execution identity by content at that instant. Both are revalidated after every callback
    /// that runs application code and before the verdict is published. A synthetic guest is distinct from
    /// the ambient unauthenticated caller, but cannot certify an authenticated execution identity.
    /// </summary>
    /// <param name="request">The prepared request.</param>
    /// <param name="needsSelectedScope">Whether the selected principal runs in its own scope.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The certified identities, or null when access is denied.</returns>
    async Task<PolicyCertification?> EvaluatePolicy(PreparedAuthorizationRequest request, bool needsSelectedScope, CancellationToken cancellationToken)
    {
        var (target, resource, services, _, prepared) = request;
        var selectedPrincipal = prepared.SelectedPrincipal;
        if (selectedPrincipal is null)
        {
            return null;
        }

        var certification = CaptureIdentities(prepared, selectedPrincipal, needsSelectedScope);
        if (certification is null)
        {
            return null;
        }

        var context = new AuthorizationPolicyContext(selectedPrincipal, target, resource!)
        {
            PrincipalAccessor = principalAccessor,
            ReceivedAt = ReceivedAtFor(resource, services)
        };
        if (!await prepared.Resolution.IsAuthorized(context, services, cancellationToken))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return StillCertified(certification, target, prepared.Declaration, selectedPrincipal) ? certification : null;
    }

    PolicyCertification? CaptureIdentities(PreparedAuthorization prepared, ClaimsPrincipal selectedPrincipal, bool needsSelectedScope)
    {
        var guest = prepared.EvaluatesAnonymous && selectedPrincipal.Identity?.IsAuthenticated != true;
        var executionPrincipal = needsSelectedScope ? selectedPrincipal : principalAccessor.Current;
        var policyIdentity = AuthorizationPrincipalIdentity.Capture(selectedPrincipal);
        var executionIdentity = AuthorizationPrincipalIdentity.Capture(executionPrincipal);
        var conflicts = PolicyInputDiffers(needsSelectedScope || guest, policyIdentity, executionPrincipal) || GuestConflicts(guest, selectedPrincipal, executionPrincipal);
        return conflicts ? null : new PolicyCertification(policyIdentity, executionIdentity, guest);
    }

    bool StillCertified(PolicyCertification certification, MemberInfo target, AuthorizationDeclaration declaration, ClaimsPrincipal selectedPrincipal) =>
        IdentityUnchanged(certification.Policy, certification.Execution, selectedPrincipal, certification.Guest) && DeclarationUnchanged(target, declaration);

    bool IdentityUnchanged(PrincipalSnapshot policyIdentity, PrincipalSnapshot executionIdentity, ClaimsPrincipal selectedPrincipal, bool guest) =>
        AuthorizationPrincipalIdentity.Same(policyIdentity, selectedPrincipal) &&
        AuthorizationPrincipalIdentity.Same(executionIdentity, principalAccessor.Current) &&
        (!guest || !AuthorizationEvaluator.HasAuthenticatedIdentity(principalAccessor.Current));

    sealed record PolicyCertification(PrincipalSnapshot Policy, PrincipalSnapshot Execution, bool Guest);
}
