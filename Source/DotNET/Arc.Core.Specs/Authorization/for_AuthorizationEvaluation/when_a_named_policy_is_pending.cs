// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_a_named_policy_is_pending : Specification
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<bool> _evaluation;
    bool _legacyCalled;

    void Establish()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test"));
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(principal);
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var evaluator = Substitute.For<IAuthorizationEvaluator>();
        evaluator.IsAuthorized(typeof(ProtectedCommand)).Returns(_ =>
        {
            _legacyCalled = true;
            return true;
        });
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        var runtime = new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Waiting", typeof(WaitingPolicy))]);
        var policy = new WaitingPolicy(_entered, _released);
        var services = new ServiceCollection().AddSingleton(policy).BuildServiceProvider();
        var evaluation = new AuthorizationEvaluation(declarations, evaluator, accessor, runtime);
        _evaluation = evaluation.IsAuthorized(typeof(ProtectedCommand), new ProtectedCommand(), services, CancellationToken.None);
    }

    async Task Because()
    {
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_not_complete_while_the_policy_is_pending() => _evaluation.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_call_the_legacy_evaluator_before_the_policy() => _legacyCalled.ShouldBeFalse();

    async Task Destroy()
    {
        _released.TrySetResult();
        await _evaluation;
    }

    [Authorize(Policy = "Waiting")]
    public record ProtectedCommand;

    public class WaitingPolicy(TaskCompletionSource entered, TaskCompletionSource released) : IAuthorizationPolicy
    {
        public async ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            entered.TrySetResult();
            await released.Task.WaitAsync(cancellationToken);
            return true;
        }
    }
}
