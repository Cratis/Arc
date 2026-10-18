// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_event_collection_predicates;

public class with_order_insensitive_comparison : given.an_event_collection_predicate
{
    void Because() => GenerateExpectation("e.Tags.OrderBy(tag => tag.Value).SequenceEqual<ApprovalLabel>([Urgent, \"review\"])");

    [Fact] void should_not_state_a_sorted_comparison_as_authored_order() => AssertUnread();
}
