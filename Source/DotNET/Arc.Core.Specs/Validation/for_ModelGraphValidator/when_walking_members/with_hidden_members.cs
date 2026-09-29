// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_walking_members;

/// <summary>
/// A member hidden with the same type is walked once, from the derived type; one hidden with another type is walked
/// twice, derived first, under the same path.
/// </summary>
public class with_hidden_members : given.a_recording_model_graph_validator
{
    void Because() => Walk(new Derived());

    [Fact] void should_walk_the_hiding_member_and_a_differently_typed_hidden_member() =>
        Traversal.ShouldEqual("Derived@ | String@sameType | String@otherType | Int32@otherType");

    class Base
    {
        public string SameType { get; set; } = "base";
        public int OtherType { get; set; } = 42;
    }

    class Derived : Base
    {
        public new string SameType { get; set; } = "derived";
        public new string OtherType { get; set; } = "other";
    }
}
