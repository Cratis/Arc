// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Checks policy input and ambient identity at each policy boundary using the verdict's identity comparison.
/// </summary>
internal sealed class AuthorizationPolicyIdentityCheckpoint
{
    readonly AuthorizationPolicyContext _context;
    readonly PrincipalSnapshot _policyIdentity;
    readonly ICurrentPrincipalAccessor? _principalAccessor;
    readonly PrincipalSnapshot? _executionIdentity;

    /// <summary>
    /// Captures both policy input and ambient execution identity before policy construction or invocation.
    /// </summary>
    /// <param name="context">The policy input and pipeline accessor.</param>
    /// <param name="services">The executing scope for direct runtime calls.</param>
    internal AuthorizationPolicyIdentityCheckpoint(AuthorizationPolicyContext context, IServiceProvider services)
    {
        _context = context;
        _policyIdentity = AuthorizationPrincipalIdentity.Capture(context.Principal);
        _principalAccessor = context.PrincipalAccessor ?? services.GetService<ICurrentPrincipalAccessor>();
        if (_principalAccessor is not null)
        {
            // Usually the policy input is the ambient principal itself; one snapshot then describes both.
            var ambient = _principalAccessor.Current;
            _executionIdentity = ReferenceEquals(ambient, context.Principal)
                ? _policyIdentity
                : AuthorizationPrincipalIdentity.Capture(ambient);
        }
    }

    /// <summary>
    /// Checks that neither captured identity has changed.
    /// </summary>
    /// <returns>Whether policy input and ambient execution identity still match their snapshots.</returns>
    internal bool IsUnchanged()
    {
        var policyUnchanged = AuthorizationPrincipalIdentity.Same(_policyIdentity, _context.Principal);
        if (!policyUnchanged || _principalAccessor is null)
        {
            return policyUnchanged;
        }

        var ambient = _principalAccessor.Current;

        // Comparing the same snapshot with the same principal again cannot give a different answer.
        return (ReferenceEquals(_executionIdentity, _policyIdentity) && ReferenceEquals(ambient, _context.Principal)) ||
               AuthorizationPrincipalIdentity.Same(_executionIdentity!, ambient);
    }
}
