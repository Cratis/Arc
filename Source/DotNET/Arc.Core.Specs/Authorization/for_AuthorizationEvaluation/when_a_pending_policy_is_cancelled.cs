// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_a_pending_policy_is_cancelled : Specification
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? _failure;
    IAuthorizationEvaluator _legacyEvaluator;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test")));
        _legacyEvaluator = Substitute.For<IAuthorizationEvaluator>();
        using var cancellation = new CancellationTokenSource();
        await using var services = new ServiceCollection().AddSingleton(new CancellingPolicy(_entered)).BuildServiceProvider();
        var evaluation = new AuthorizationEvaluation(
            new AuthorizationDeclarations(anonymous, attributes),
            _legacyEvaluator,
            accessor,
            new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Cancelling", typeof(CancellingPolicy))]));
        var pending = evaluation.IsAuthorized(typeof(ProtectedCommand), new ProtectedCommand(), services, cancellation.Token);
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cancellation.CancelAsync();
        _failure = await Catch.Exception(() => pending);
    }

    [Fact] void should_cancel_authorization() => _failure.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_not_call_the_legacy_evaluator() => _legacyEvaluator.DidNotReceive().IsAuthorized(typeof(ProtectedCommand));

    [Authorize(Policy = "Cancelling")]
    public record ProtectedCommand;

    public class CancellingPolicy(TaskCompletionSource entered) : IAuthorizationPolicy
    {
        public async ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            entered.TrySetResult();
            var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var registration = cancellationToken.Register(() => cancelled.TrySetCanceled(cancellationToken));
            await cancelled.Task;
            return true;
        }
    }
}
