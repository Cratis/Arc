// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Projections;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.for_ProjectionKeyConverter;

/// <summary>
/// The key decides which read model instance an event maps onto, so losing it silently would misstate the model.
/// Composite keys are written in two shapes and both have to be understood.
/// </summary>
public class when_converting_a_composite_key : Specification
{
    CompositeKeySyntax _withoutATypeName;
    CompositeKeySyntax _withATypeName;
    KeySyntax? _unconvertible;

    void Because()
    {
        _withoutATypeName = (CompositeKeySyntax)ProjectionKeyConverter.Convert("$composite(CustomerId=customerId,OrderNumber=orderNumber)", "Order")!;
        _withATypeName = (CompositeKeySyntax)ProjectionKeyConverter.Convert("$composite(OrderKey, CustomerId=customerId, OrderNumber=orderNumber)", "Order")!;
        _unconvertible = ProjectionKeyConverter.Convert("$composite(CustomerId=$unknownExpression)", "Order");
    }

    [Fact] void should_read_every_part_of_the_shape_without_a_type_name() => _withoutATypeName.Parts.Select(_ => _.Property).ShouldContainOnly(["CustomerId", "OrderNumber"]);
    [Fact] void should_fall_back_to_the_read_model_name_when_no_type_is_carried() => _withoutATypeName.Type.ShouldEqual("Order");
    [Fact] void should_read_every_part_of_the_shape_with_a_type_name() => _withATypeName.Parts.Select(_ => _.Property).ShouldContainOnly(["CustomerId", "OrderNumber"]);
    [Fact] void should_use_the_type_the_shape_carries() => _withATypeName.Type.ShouldEqual("OrderKey");
    [Fact] void should_drop_a_composite_whose_part_cannot_be_expressed() => _unconvertible.ShouldBeNull();
}
