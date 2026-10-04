// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_subscribing_to_an_ordered_collection;

public class and_an_item_changes_in_place : given.a_delta_subscription
{
    void Establish() => SortByName();

    Task Because() => Emit(
        [new Item(1, "A"), new Item(2, "B")],
        [new Item(1, "Aa"), new Item(2, "B"), new Item(3, "C")]);

    [Fact] void should_omit_the_data() => _results[1].Data.ShouldBeNull();
    [Fact] void should_send_the_replaced_item() => _results[1].ChangeSet!.Replaced.Cast<Item>().Single().Name.ShouldEqual("Aa");
    [Fact] void should_send_the_item_added_at_the_end() => _results[1].ChangeSet!.Added.Cast<Item>().Single().Id.ShouldEqual(3);
}
