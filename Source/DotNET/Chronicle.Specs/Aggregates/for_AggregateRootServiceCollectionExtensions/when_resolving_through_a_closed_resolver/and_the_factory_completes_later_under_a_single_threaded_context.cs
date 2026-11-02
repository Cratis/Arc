// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootServiceCollectionExtensions.when_resolving_through_a_closed_resolver;

public class and_the_factory_completes_later_under_a_single_threaded_context : Specification
{
    IAggregateRootFactory _factory;
    SampleAggregateRoot _aggregateRoot;
    TaskCompletionSource<SampleAggregateRoot> _completion;
    object _result;
    bool _finished;

    void Establish()
    {
        _aggregateRoot = new SampleAggregateRoot();
        _completion = new TaskCompletionSource<SampleAggregateRoot>();
        _factory = Substitute.For<IAggregateRootFactory>();
        _factory.Get<SampleAggregateRoot>(Arg.Any<EventSourceId>(), null, null).Returns(_completion.Task);
    }

    void Because()
    {
        // The resolving thread blocks on the factory task while a context that never runs posted work is current, the
        // way a single-threaded synchronization context does while its only thread waits.
        var resolving = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new BlockedSynchronizationContext());
            _result = AggregateRootServiceCollectionExtensions.GetAggregateRoot(
                AggregateRootServiceCollectionExtensions.CloseResolverFor(typeof(SampleAggregateRoot)),
                _factory,
                EventSourceId.New());
        })
        {
            IsBackground = true
        };
        resolving.Start();
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            _completion.SetResult(_aggregateRoot);
        });
        _finished = resolving.Join(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_finish_resolving() => _finished.ShouldBeTrue();
    [Fact] void should_return_the_aggregate_root_itself() => _result.ShouldEqual(_aggregateRoot);

    sealed class BlockedSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
        }
    }
}
