// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using AspNetAuthorizationResult = Microsoft.AspNetCore.Authorization.AuthorizationResult;

namespace Cratis.Arc.Authorization.for_AspNetAuthorizationPolicyRuntime;

public class when_a_native_policy_changes_identity_before_an_aspnet_policy : Specification
{
    bool _allowed;
    bool _aspPolicyCalled;

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("subject", "original")], "test"));
        var native = new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Native", typeof(MutatingPolicy))]);
        var runtime = new AspNetAuthorizationPolicyRuntime(native);
        var provider = Substitute.For<IAuthorizationPolicyProvider>();
        provider.GetPolicyAsync("AspNet").Returns(Task.FromResult<AuthorizationPolicy?>(new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build()));
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(_ =>
            {
                _aspPolicyCalled = true;
                return Task.FromResult(AspNetAuthorizationResult.Success());
            });
        await using var services = new ServiceCollection()
            .AddSingleton(provider)
            .AddSingleton(authorization)
            .AddSingleton(new MutatingPolicy())
            .BuildServiceProvider();
        AuthorizationRequirement[] requirements = [
            AuthorizationRequirement.FromAttribute(null, "Native", null),
            AuthorizationRequirement.FromAttribute(null, "AspNet", null)
        ];
        var resolution = await runtime.Resolve(requirements, services, CancellationToken.None);
        _allowed = await resolution.IsAuthorized(new AuthorizationPolicyContext(principal, typeof(object), new object()), services, CancellationToken.None);
    }

    [Fact] void should_deny_the_mutated_identity() => _allowed.ShouldBeFalse();
    [Fact] void should_not_call_the_aspnet_policy() => _aspPolicyCalled.ShouldBeFalse();

    public class MutatingPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            context.Principal.Identities.Single().AddClaim(new Claim("subject", "changed"));
            return ValueTask.FromResult(true);
        }
    }
}
