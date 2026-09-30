// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_walking_members;

/// <summary>
/// Pins the order members are walked in: the runtime type's own members in declaration order, then each base type's,
/// with an override walked once, where the derived type declares it.
/// </summary>
public class with_inherited_members : given.a_recording_model_graph_validator
{
    void Because() => Walk(new Derived());

    [Fact] void should_walk_own_members_then_base_members() =>
        Traversal.ShouldEqual("Derived@ | String@own | String@overridden | String@first | String@last");

    class Base
    {
        public string First { get; set; } = "first";
        public virtual string Overridden { get; set; } = "base";
        public string Last { get; set; } = "last";
    }

    class Derived : Base
    {
        public string Own { get; set; } = "own";
        public override string Overridden { get; set; } = "derived";
    }
}
