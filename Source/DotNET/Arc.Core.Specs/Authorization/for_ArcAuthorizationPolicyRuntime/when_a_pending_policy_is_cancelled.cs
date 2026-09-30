// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_ArcAuthorizationPolicyRuntime;

public class when_a_pending_policy_is_cancelled : Specification
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _laterCalled;
    Exception? _error;

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("subject", "original")], "test"));
        await using var services = new ServiceCollection()
            .AddSingleton(new WaitingPolicy(_entered, _released))
            .AddSingleton(new LaterPolicy(() => _laterCalled = true))
            .BuildServiceProvider();
        var runtime = new ArcAuthorizationPolicyRuntime([
            new AuthorizationPolicyRegistration("Waiting", typeof(WaitingPolicy)),
            new AuthorizationPolicyRegistration("Later", typeof(LaterPolicy))
        ]);
        AuthorizationRequirement[] requirements = [
            AuthorizationRequirement.FromAttribute(null, "Waiting", null),
            AuthorizationRequirement.FromAttribute(null, "Later", null)
        ];
        var resolution = await runtime.Resolve(requirements, services, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var evaluation = resolution.IsAuthorized(new AuthorizationPolicyContext(principal, typeof(object), new object()), services, cancellation.Token);
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cancellation.CancelAsync();
        _error = await Catch.Exception(() => evaluation);
    }

    [Fact] void should_propagate_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_run_a_later_policy() => _laterCalled.ShouldBeFalse();

    public class WaitingPolicy(TaskCompletionSource entered, TaskCompletionSource released) : IAuthorizationPolicy
    {
        public async ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            entered.TrySetResult();
            await released.Task.WaitAsync(cancellationToken);
            return true;
        }
    }

    public class LaterPolicy(Action called) : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            called();
            return ValueTask.FromResult(true);
        }
    }
}
