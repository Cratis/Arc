// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation.given;

public class a_named_policy_evaluation : Specification
{
    protected IAuthorizationEvaluator _legacyEvaluator;
    protected AuthorizationEvaluation _evaluation;
    protected IServiceProvider _services;

    void Establish()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test"));
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(principal);
        _legacyEvaluator = Substitute.For<IAuthorizationEvaluator>();
        _services = new ServiceCollection().AddSingleton<AllowingPolicy>().AddSingleton<DenyingPolicy>().BuildServiceProvider();
        _evaluation = new AuthorizationEvaluation(
            new AuthorizationDeclarations(anonymous, attributes),
            _legacyEvaluator,
            accessor,
            new ArcAuthorizationPolicyRuntime(
            [
                new AuthorizationPolicyRegistration("Allowing", typeof(AllowingPolicy)),
                new AuthorizationPolicyRegistration("Denying", typeof(DenyingPolicy))
            ]));
    }

    protected Task<bool> Authorize() => _evaluation.IsAuthorized(
        typeof(ProtectedCommand),
        new ProtectedCommand(),
        _services,
        CancellationToken.None);

    protected Task<bool> AuthorizeDenied() => _evaluation.IsAuthorized(
        typeof(ProtectedDeniedCommand),
        new ProtectedDeniedCommand(),
        _services,
        CancellationToken.None);

    [Authorize(Policy = "Allowing")]
    public record ProtectedCommand;

    [Authorize(Policy = "Denying")]
    public record ProtectedDeniedCommand;

    public class AllowingPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }

    public class DenyingPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(false);
    }
}
