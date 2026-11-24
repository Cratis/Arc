// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityExpression;

public class when_expanding_with : Specification
{
    string _result;

    void Because()
    {
        var catalog = new AvailabilityCatalog()
            .Add(AvailabilityDefinition.FromLines("Staff", AvailabilityExpression.For("Admin", "Operator")))
            .Add(AvailabilityDefinition.FromLines("Nested", AvailabilityExpression.With("Staff"), AvailabilityExpression.For("Beta")));
        _result = AlternativeText.Of(AvailabilityExpression.AnyOf(AvailabilityExpression.With("Nested"), AvailabilityExpression.For("Owner")).Alternatives(catalog));
    }

    [Fact] void should_replace_the_use_by_the_block_recursively() => _result.ShouldEqual("Admin+Beta|Operator+Beta|Owner");
}
