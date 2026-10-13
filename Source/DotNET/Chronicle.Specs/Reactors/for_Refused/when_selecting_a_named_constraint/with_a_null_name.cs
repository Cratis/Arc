// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors.for_Refused.when_selecting_a_named_constraint;

public class with_a_null_name : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => Refused.ByConstraint(null!));

    [Fact] void should_require_a_constraint_name() => _error.ShouldBeOfExactType<ConstraintNameIsRequired>();
}
