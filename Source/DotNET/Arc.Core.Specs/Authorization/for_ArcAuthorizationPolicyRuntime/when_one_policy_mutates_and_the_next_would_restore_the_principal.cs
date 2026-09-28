// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_ArcAuthorizationPolicyRuntime;

public class when_one_policy_mutates_and_the_next_would_restore_the_principal : Specification
{
    bool _allowed;
    bool _restorerCalled;

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("subject", "original")], "test"));
        var identity = principal.Identities.Single();
        var original = identity.Claims.Single();
        var runtime = new ArcAuthorizationPolicyRuntime([
            new AuthorizationPolicyRegistration("Mutate", typeof(MutatingPolicy)),
            new AuthorizationPolicyRegistration("Restore", typeof(RestoringPolicy))
        ]);
        await using var services = new ServiceCollection()
            .AddSingleton(new MutatingPolicy(() =>
            {
                identity.RemoveClaim(original);
                identity.AddClaim(new Claim("subject", "changed"));
            }))
            .AddSingleton(new RestoringPolicy(() =>
            {
                _restorerCalled = true;
                identity.RemoveClaim(identity.Claims.Single());
                identity.AddClaim(original);
            }))
            .BuildServiceProvider();
        AuthorizationRequirement[] requirements = [
            AuthorizationRequirement.FromAttribute(null, "Mutate", null),
            AuthorizationRequirement.FromAttribute(null, "Restore", null)
        ];
        var resolution = await runtime.Resolve(requirements, services, CancellationToken.None);
        _allowed = await resolution.IsAuthorized(new AuthorizationPolicyContext(principal, typeof(object), new object()), services, CancellationToken.None);
    }

    [Fact] void should_deny_the_mutated_identity() => _allowed.ShouldBeFalse();
    [Fact] void should_never_call_the_restoring_policy() => _restorerCalled.ShouldBeFalse();

    public class MutatingPolicy(Action mutate) : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            mutate();
            return ValueTask.FromResult(true);
        }
    }

    public class RestoringPolicy(Action restore) : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            restore();
            return ValueTask.FromResult(true);
        }
    }
}
