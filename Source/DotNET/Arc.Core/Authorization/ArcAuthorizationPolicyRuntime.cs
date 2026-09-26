// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Evaluates native Arc policies; the standalone host cannot authenticate named schemes.
/// </summary>
/// <param name="registrations">Named policy registrations.</param>
public class ArcAuthorizationPolicyRuntime(IEnumerable<AuthorizationPolicyRegistration> registrations) : IAuthorizationPolicyRuntime
{
    readonly AuthorizationPolicyRegistration[] _registrations = [.. registrations];

    /// <inheritdoc/>
    public async Task Validate(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken) =>
        _ = await Resolve(requirements, services, cancellationToken);

    /// <inheritdoc/>
    public Task<IAuthorizationPolicyResolution> Resolve(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var requirement in requirements)
        {
            if (requirement.AuthenticationSchemes.Count > 0)
            {
                throw new InvalidAuthorizationConfiguration("AuthenticationSchemes requires the ASP.NET Core Arc host.");
            }

            if (!string.IsNullOrWhiteSpace(requirement.Policy) && !HasPolicy(requirement.Policy))
            {
                throw new InvalidAuthorizationConfiguration($"Unknown authorization policy '{requirement.Policy}'.");
            }
        }

        IAuthorizationPolicyResolution resolution = new NativeResolution(this, [.. requirements]);
        return Task.FromResult(resolution);
    }

    /// <summary>
    /// Checks whether a name identifies exactly one native policy.
    /// </summary>
    /// <param name="name">The policy name.</param>
    /// <returns>Whether it is registered.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">The policy name is ambiguous.</exception>
    public bool HasPolicy(string name)
    {
        var matches = _registrations.Where(registration => registration.Name == name).ToArray();
        if (matches.Length > 1)
        {
            throw new InvalidAuthorizationConfiguration($"Authorization policy '{name}' has multiple registrations.");
        }

        return matches.Length == 1;
    }

    AuthorizationPolicyRegistration PolicyFor(string name) =>
        _registrations.Single(registration => registration.Name == name);

    sealed class NativeResolution(ArcAuthorizationPolicyRuntime runtime, AuthorizationRequirement[] requirements) : IAuthorizationPolicyResolution, IAnonymousPolicyResolution
    {
        public bool EvaluatesAnonymous => requirements.Length > 0 && requirements.All(requirement =>
            requirement.AnyOfRoles.Count == 0 && requirement.AuthenticationSchemes.Count == 0 &&
            !string.IsNullOrWhiteSpace(requirement.Policy) && runtime.PolicyFor(requirement.Policy).EvaluatesAnonymous);

        public Task<ClaimsPrincipal?> SelectPrincipal(ClaimsPrincipal? principal, IServiceProvider services, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(principal);
        }

        public async Task<bool> IsAuthorized(AuthorizationPolicyContext context, IServiceProvider services, CancellationToken cancellationToken)
        {
            foreach (var requirement in requirements.Where(requirement => !string.IsNullOrWhiteSpace(requirement.Policy)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var policy = (IAuthorizationPolicy)services.GetRequiredService(runtime.PolicyFor(requirement.Policy!).PolicyType);
                if (!await policy.IsAuthorized(context, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return false;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }
    }
}
