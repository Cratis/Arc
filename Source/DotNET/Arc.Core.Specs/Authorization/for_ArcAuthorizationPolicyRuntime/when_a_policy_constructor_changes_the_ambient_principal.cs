// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_ArcAuthorizationPolicyRuntime;

public class when_a_policy_constructor_changes_the_ambient_principal : Specification
{
    bool _allowed;
    bool _policyCalled;

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("subject", "original")], "test"));
        var ambient = new ClaimsPrincipal(new ClaimsIdentity([new Claim("execution", "original")], "test"));
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(ambient);
        await using var services = new ServiceCollection()
            .AddSingleton<ICurrentPrincipalAccessor>(accessor)
            .AddTransient(_ => new ConstructedPolicy(
                () => ambient.Identities.Single().AddClaim(new Claim("execution", "changed")),
                () => _policyCalled = true))
            .BuildServiceProvider();
        var runtime = new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Construct", typeof(ConstructedPolicy))]);
        var resolution = await runtime.Resolve([AuthorizationRequirement.FromAttribute(null, "Construct", null)], services, CancellationToken.None);
        _allowed = await resolution.IsAuthorized(new AuthorizationPolicyContext(principal, typeof(object), new object()), services, CancellationToken.None);
    }

    [Fact] void should_deny_the_constructor_mutation() => _allowed.ShouldBeFalse();
    [Fact] void should_not_invoke_the_constructed_policy() => _policyCalled.ShouldBeFalse();

    public class ConstructedPolicy : IAuthorizationPolicy
    {
        readonly Action _invoke;

        public ConstructedPolicy(Action mutate, Action invoke)
        {
            mutate();
            _invoke = invoke;
        }

        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            _invoke();
            return ValueTask.FromResult(true);
        }
    }
}
