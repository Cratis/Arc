// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_event_collection_predicates;

public class with_a_collection_expression : given.an_event_collection_predicate
{
    void Because() => GenerateExpectation("e.Tags.SequenceEqual<ApprovalLabel>([Urgent, \"review\"])");

    [Fact] void should_state_and_bind_the_expected_concepts_in_order() => AssertCollection("[\"urgent\",\"review\"]");
}
