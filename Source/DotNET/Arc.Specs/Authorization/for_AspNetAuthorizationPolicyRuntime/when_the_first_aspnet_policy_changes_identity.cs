// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using AspNetAuthorizationResult = Microsoft.AspNetCore.Authorization.AuthorizationResult;

namespace Cratis.Arc.Authorization.for_AspNetAuthorizationPolicyRuntime;

public class when_the_first_aspnet_policy_changes_identity : Specification
{
    bool _allowed;
    int _policyCalls;
    bool _resourceUnchanged;

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("subject", "original")], "test"));
        var original = principal.Identities.Single().Claims.Single();
        var resource = new object();
        var first = new AuthorizationPolicyBuilder().RequireClaim("policy", "first").Build();
        var second = new AuthorizationPolicyBuilder().RequireClaim("policy", "second").Build();
        var provider = Substitute.For<IAuthorizationPolicyProvider>();
        provider.GetPolicyAsync("First").Returns(Task.FromResult<AuthorizationPolicy?>(first));
        provider.GetPolicyAsync("Second").Returns(Task.FromResult<AuthorizationPolicy?>(second));
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(call =>
            {
                _policyCalls++;
                var identity = call.Arg<ClaimsPrincipal>().Identities.Single();
                if (_policyCalls == 1)
                {
                    identity.RemoveClaim(original);
                    identity.AddClaim(new Claim("subject", "changed"));
                }
                else
                {
                    identity.RemoveClaim(identity.Claims.Single());
                    identity.AddClaim(original);
                }

                _resourceUnchanged = ReferenceEquals(call.ArgAt<object>(1), resource);
                return Task.FromResult(AspNetAuthorizationResult.Success());
            });
        await using var services = new ServiceCollection().AddSingleton(provider).AddSingleton(authorization).BuildServiceProvider();
        var runtime = new AspNetAuthorizationPolicyRuntime(new ArcAuthorizationPolicyRuntime([]));
        AuthorizationRequirement[] requirements = [
            AuthorizationRequirement.FromAttribute(null, "First", null),
            AuthorizationRequirement.FromAttribute(null, "Second", null)
        ];
        var resolution = await runtime.Resolve(requirements, services, CancellationToken.None);
        _allowed = await resolution.IsAuthorized(new AuthorizationPolicyContext(principal, typeof(object), resource) { ReceivedAt = DateTimeOffset.UnixEpoch }, services, CancellationToken.None);
    }

    [Fact] void should_deny_the_mutated_identity() => _allowed.ShouldBeFalse();
    [Fact] void should_not_call_the_restoring_policy() => _policyCalls.ShouldEqual(1);
    [Fact] void should_preserve_the_resource() => _resourceUnchanged.ShouldBeTrue();
}
