// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_collection_fixtures;

public class with_a_computed_element : given.a_collection_fixture
{
    void Because() => GenerateFixture("ApprovalLabel", "[Urgent, Computed()]");

    [Fact] void should_leave_out_the_scenario_rather_than_state_a_partial_list() => AssertUnread();
}
