// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe.for_EntityChangeTracker.given;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_EntityChangeTracker.when_registering_callback;

#pragma warning disable SA1402, SA1649 // File may only contain a single type, File name should match first type name

public class OtherEntity
{
    public int Id { get; set; }
}

#pragma warning restore SA1402, SA1649

public class and_notifying_change_for_different_entity_type : an_entity_change_tracker
{
    int _callbackCount;
    IDisposable _subscription;

    void Establish()
    {
        _callbackCount = 0;
        _subscription = _changeTracker.RegisterCallback<TestEntity>(() => _callbackCount++);
    }

    void Because() => _changeTracker.NotifyChange(typeof(OtherEntity));

    [Fact] void should_not_invoke_the_callback() => _callbackCount.ShouldEqual(0);

    void Cleanup() => _subscription?.Dispose();
}
