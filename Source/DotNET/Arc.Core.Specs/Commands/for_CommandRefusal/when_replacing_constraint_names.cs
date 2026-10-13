// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandRefusal;

public class when_replacing_constraint_names : Specification
{
    CommandRefusal _original;
    CommandRefusal _replaced;
    CommandRefusal _cleared;

    void Establish() => _original = new(CommandRefusalKind.Constraint, "Original", "refused");

    void Because()
    {
        _replaced = _original with { ConstraintNames = ["First", "Second"] };
        _cleared = _original with { ConstraintNames = [] };
    }

    [Fact] void should_derive_the_constraint_name_from_the_first_name() => _replaced.ConstraintName.ShouldEqual("First");
    [Fact] void should_have_no_constraint_name_without_names() => _cleared.ConstraintName.ShouldBeNull();
    [Fact] void should_preserve_the_original_constraint_name() => _original.ConstraintName.ShouldEqual("Original");
}
