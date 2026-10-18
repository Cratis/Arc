// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_event_collection_predicates;

public class with_a_computed_element : given.an_event_collection_predicate
{
    void Because() => GenerateExpectation("e.Tags.SequenceEqual<ApprovalLabel>([Urgent, Computed()])");

    [Fact] void should_not_state_a_partial_expected_collection() => AssertUnread();
}
