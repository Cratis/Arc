// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_a_contained_event;

public class and_the_decision_is_a_string : Specification
{
    string _matched;
    void Because() => _matched = "decision".ShouldContainEvent<string>();
    [Fact] void should_not_split_it_into_characters() => _matched.ShouldEqual("decision");
}
