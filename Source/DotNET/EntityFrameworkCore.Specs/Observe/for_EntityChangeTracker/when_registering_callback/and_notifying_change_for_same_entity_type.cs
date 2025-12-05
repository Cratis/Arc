// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe.for_EntityChangeTracker.given;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_EntityChangeTracker.when_registering_callback;

public class and_notifying_change_for_same_entity_type : an_entity_change_tracker
{
    int _callbackCount;
    IDisposable _subscription;

    void Establish()
    {
        _callbackCount = 0;
        _subscription = _changeTracker.RegisterCallback<TestEntity>(() => _callbackCount++);
    }

    void Because() => _changeTracker.NotifyChange(typeof(TestEntity));

    [Fact] void should_invoke_the_callback() => _callbackCount.ShouldEqual(1);

    void Cleanup() => _subscription?.Dispose();
}
