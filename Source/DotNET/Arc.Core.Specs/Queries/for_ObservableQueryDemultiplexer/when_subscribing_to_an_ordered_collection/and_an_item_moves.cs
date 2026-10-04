// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_subscribing_to_an_ordered_collection;

public class and_an_item_moves : given.a_delta_subscription
{
    void Establish() => SortByName();

    Task Because() => Emit(
        [new Item(1, "A"), new Item(2, "B")],
        [new Item(2, "B"), new Item(1, "C")]);

    [Fact] void should_send_the_snapshot() => Assert.Equal([2, 1], IdsIn(_results[1]));
    [Fact] void should_not_send_a_change_set() => _results[1].ChangeSet.ShouldBeNull();
}
