// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_ArcAuthorizationPolicyRuntime;

public class when_multiple_policies_preserve_identity : Specification
{
    bool _allowed;
    int _calls;

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("subject", "original")], "test"));
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(principal);
        await using var services = new ServiceCollection()
            .AddSingleton<ICurrentPrincipalAccessor>(accessor)
            .AddSingleton(new FirstPolicy(() => _calls++))
            .AddSingleton(new SecondPolicy(() => _calls++))
            .BuildServiceProvider();
        var runtime = new ArcAuthorizationPolicyRuntime([
            new AuthorizationPolicyRegistration("First", typeof(FirstPolicy)),
            new AuthorizationPolicyRegistration("Second", typeof(SecondPolicy))
        ]);
        AuthorizationRequirement[] requirements = [
            AuthorizationRequirement.FromAttribute(null, "First", null),
            AuthorizationRequirement.FromAttribute(null, "Second", null)
        ];
        var resolution = await runtime.Resolve(requirements, services, CancellationToken.None);
        _allowed = await resolution.IsAuthorized(new AuthorizationPolicyContext(principal, typeof(object), new object()), services, CancellationToken.None);
    }

    [Fact] void should_allow_both_policies() => _allowed.ShouldBeTrue();
    [Fact] void should_invoke_each_policy_once() => _calls.ShouldEqual(2);

    public class FirstPolicy(Action called) : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            called();
            return ValueTask.FromResult(true);
        }
    }

    public class SecondPolicy(Action called) : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            called();
            return ValueTask.FromResult(true);
        }
    }
}
