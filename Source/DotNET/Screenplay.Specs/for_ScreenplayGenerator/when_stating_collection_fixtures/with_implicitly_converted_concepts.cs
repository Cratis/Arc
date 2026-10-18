// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_collection_fixtures;

public class with_implicitly_converted_concepts : given.a_collection_fixture
{
    void Because() => GenerateFixture("ApprovalLabel", "[Urgent, \"review\"]");

    [Fact] void should_state_and_bind_the_concepts_backing_values() => AssertTags("[\"urgent\",\"review\"]");
}
