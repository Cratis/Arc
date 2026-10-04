// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer;

public class when_subscribing_to_an_unordered_collection_whose_items_move : given.a_delta_subscription
{
    Task Because() => Emit(
        [new Item(1, "A"), new Item(2, "B")],
        [new Item(2, "B"), new Item(1, "C")]);

    [Fact] void should_omit_the_data() => _results[1].Data.ShouldBeNull();
    [Fact] void should_send_the_change_set() => _results[1].ChangeSet!.Replaced.Cast<Item>().Single().Id.ShouldEqual(1);
}
