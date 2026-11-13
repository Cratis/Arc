// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Contexts.for_ContextValues;

public class when_reading_the_values_as_a_type : given.a_provided_vessel
{
    VesselValues _values;

    void Because() => _values = ContextValues.For(_services, () => null).ValuesAs<VesselValues>();

    [Fact] void should_fill_the_values_by_name() => _values.Vessel.ShouldEqual("Aurora");
    [Fact] void should_fill_a_nullable_value() => _values.Draft.ShouldEqual(12);
}
