// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_collection_fixtures;

public class with_a_list_initializer : given.a_collection_fixture
{
    void Because() => GenerateFixture("ApprovalLabel", "new List<ApprovalLabel> { Urgent, new ApprovalLabel(\"review\") }");

    [Fact] void should_state_and_bind_the_list() => AssertTags("[\"urgent\",\"review\"]");
}
