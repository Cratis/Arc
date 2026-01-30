// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe.for_EntityChangeTracker.given;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_EntityChangeTracker.when_registering_callback;

public class and_disposing_subscription_before_notifying : an_entity_change_tracker
{
    const string TableName = "TestEntities";
    int _callbackCount;
    IDisposable _subscription;

    void Establish()
    {
        _callbackCount = 0;
        _subscription = _changeTracker.RegisterCallback(TableName, () => _callbackCount++);
        _subscription.Dispose();
    }

    void Because() => _changeTracker.NotifyChange(TableName);

    [Fact] void should_not_invoke_the_callback() => _callbackCount.ShouldEqual(0);
}
