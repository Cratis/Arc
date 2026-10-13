// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Reactors.for_Refused;

public class when_comparing_selectors : Specification
{
    Refused _first;
    Refused _equivalent;
    Refused _different;
    bool _equal;
    bool _differentName;
    bool _differentKinds;

    void Establish()
    {
        _first = Refused.ByConstraint("Seat");
        _equivalent = Refused.ByConstraint("Seat");
        _different = Refused.ByConstraint("seat");
    }

    void Because()
    {
        _equal = _first == _equivalent;
        _differentName = _first == _different;
        _differentKinds = Refused.Any == Refused.ByAnyConstraint;
    }

    [Fact] void should_compare_equivalent_selections_by_value() => _equal.ShouldBeTrue();
    [Fact] void should_hash_equivalent_selections_equally() => _first.GetHashCode().ShouldEqual(_equivalent.GetHashCode());
    [Fact] void should_distinguish_exact_names() => _differentName.ShouldBeFalse();
    [Fact] void should_distinguish_kinds() => _differentKinds.ShouldBeFalse();
    [Fact] void should_expose_the_selected_kinds() => _first.Kinds.ShouldEqual([CommandRefusalKind.Constraint]);
    [Fact] void should_expose_the_constraint_restriction() => _first.ConstraintName.ShouldEqual("Seat");
    [Fact] void should_expose_any_without_authorization() => Refused.Any.Kinds.ShouldEqual([CommandRefusalKind.Validation, CommandRefusalKind.Constraint]);
}
