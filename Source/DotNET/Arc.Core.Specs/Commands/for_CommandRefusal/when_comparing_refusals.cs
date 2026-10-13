// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandRefusal;

public class when_comparing_refusals : Specification
{
    CommandRefusal _first;
    CommandRefusal _equivalent;
    bool _equal;
    bool _differentNames;
    bool _differentOrder;
    bool _differentKind;
    bool _differentMessage;
    bool _null;

    void Establish()
    {
        _first = new(CommandRefusalKind.Constraint, "First", "refused") { ConstraintNames = ["First", "Second"] };
        _equivalent = new(CommandRefusalKind.Constraint, "First", "refused") { ConstraintNames = ["First", "Second"] };
    }

    void Because()
    {
        _equal = _first == _equivalent;
        _differentNames = _first == _first with { ConstraintNames = ["First", "Third"] };
        _differentOrder = _first == _first with { ConstraintNames = ["Second", "First"] };
        _differentKind = _first == _first with { Kind = CommandRefusalKind.Validation };
        _differentMessage = _first == _first with { Message = "other" };
        _null = _first.Equals(null);
    }

    [Fact] void should_compare_constraint_names_by_value() => _equal.ShouldBeTrue();
    [Fact] void should_hash_equivalent_refusals_equally() => _first.GetHashCode().ShouldEqual(_equivalent.GetHashCode());
    [Fact] void should_distinguish_constraint_names() => _differentNames.ShouldBeFalse();
    [Fact] void should_distinguish_constraint_order() => _differentOrder.ShouldBeFalse();
    [Fact] void should_distinguish_kinds() => _differentKind.ShouldBeFalse();
    [Fact] void should_distinguish_messages() => _differentMessage.ShouldBeFalse();
    [Fact] void should_not_equal_null() => _null.ShouldBeFalse();
}
