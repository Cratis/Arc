// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_collection_fixtures;

public class with_an_unrelated_empty_method : given.a_collection_fixture
{
    void Because() => GenerateFixture("string", "Empty<string>()");

    [Fact] void should_not_mistake_a_method_name_for_the_framework_contract() => AssertUnread();
}
