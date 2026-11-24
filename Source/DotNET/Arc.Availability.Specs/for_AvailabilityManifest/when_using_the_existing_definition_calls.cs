// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityManifest;

public class when_using_the_existing_definition_calls : Specification
{
    AvailabilityDefinition _constructed;
    AvailabilityDefinition _fromLines;
    AvailabilityManifest _empty;

    void Because()
    {
        _constructed = new AvailabilityDefinition("A", AvailabilityExpression.For("X"));
        _fromLines = AvailabilityDefinition.FromLines("B", AvailabilityExpression.For("X"), AvailabilityExpression.For("Y"));
        _empty = new InMemoryAvailabilityManifest().Current;
    }

    [Fact] void should_keep_the_two_argument_constructor() => _constructed.Name.ShouldEqual("A");
    [Fact] void should_default_the_description_to_empty() => _constructed.Description.ShouldEqual(string.Empty);
    [Fact] void should_keep_from_lines() => AlternativeText.Of(_fromLines.Expression.Alternatives()).ShouldEqual("X+Y");
    [Fact] void should_declare_nothing_by_default() => _empty.ShouldEqual(AvailabilityManifest.Empty);
}
