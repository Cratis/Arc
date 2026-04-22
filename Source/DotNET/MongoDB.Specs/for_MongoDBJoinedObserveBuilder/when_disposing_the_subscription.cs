// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoDBJoinedObserveBuilder;

public class when_disposing_the_subscription : given.a_joined_observe_builder
{
    IDisposable _subscription = default!;

    async Task Because()
    {
        var firstEmission = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var subject = _builder.Select((a, b) => (a, b));
        _subscription = subject.Subscribe(_ => firstEmission.TrySetResult());

        await firstEmission.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _subscription.Dispose();

        SpinWait.SpinUntil(() => !_databaseChanges.HasObservers, TimeSpan.FromSeconds(5))
            .ShouldBeTrue();
    }

    [Fact]
    void should_detach_from_database_changes() => _databaseChanges.HasObservers.ShouldBeFalse();
}