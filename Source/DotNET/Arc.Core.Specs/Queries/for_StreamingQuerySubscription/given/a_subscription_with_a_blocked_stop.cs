// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_StreamingQuerySubscription.given;

public class a_subscription_with_a_blocked_stop : Specification
{
    internal StreamingQuerySubscription _subscription;
    protected IDisposable _producer;
    protected IDisposable _resource;
    protected IServiceScope _scope;
    protected TaskCompletionSource _stopStarted;
    protected TaskCompletionSource _releaseStop;

    void Establish()
    {
        _subscription = new(CancellationToken.None);
        _producer = Substitute.For<IDisposable>();
        _resource = Substitute.For<IDisposable>();
        _scope = Substitute.For<IServiceScope>();
        _stopStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _releaseStop = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _subscription.SetProducer(_producer);
        _subscription.AddResource(_resource);
        _subscription.AddResource(_scope);
        _subscription.TryEnter().ShouldBeTrue();
    }

    protected void PauseStopping()
    {
        _stopStarted.TrySetResult();
        _releaseStop.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken).GetAwaiter().GetResult();
    }
}
