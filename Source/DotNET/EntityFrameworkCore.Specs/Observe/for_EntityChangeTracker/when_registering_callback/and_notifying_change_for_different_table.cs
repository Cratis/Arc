// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe.for_EntityChangeTracker.given;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_EntityChangeTracker.when_registering_callback;

public class and_notifying_change_for_different_table : an_entity_change_tracker
{
    const string RegisteredTable = "TestEntities";
    const string ChangedTable = "OtherEntities";
    int _callbackCount;
    IDisposable _subscription;

    void Establish()
    {
        _callbackCount = 0;
        _subscription = _changeTracker.RegisterCallback(RegisteredTable, () => _callbackCount++);
    }

    void Because() => _changeTracker.NotifyChange(ChangedTable);

    [Fact] void should_not_invoke_the_callback() => _callbackCount.ShouldEqual(0);

    void Cleanup() => _subscription?.Dispose();
}
