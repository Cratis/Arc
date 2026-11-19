// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Contexts.for_ContextValues;

public class when_reading_a_value_by_name : given.a_provided_vessel
{
    object _dynamicValue;
    string _typed;
    object _absent;

    void Because()
    {
        var values = ContextValues.For(_services, () => typeof(string));
        _dynamicValue = values.AsDynamic().Vessel;
        _typed = values.Get<string>("vessel");
        _absent = values.AsDynamic().Nothing;
    }

    [Fact] void should_read_the_value_with_a_pascal_cased_name() => _dynamicValue.ShouldEqual("Aurora");
    [Fact] void should_read_the_typed_value() => _typed.ShouldEqual("Aurora");
    [Fact] void should_read_an_absent_value_as_null() => _absent.ShouldBeNull();
    [Fact] void should_read_for_the_part() => _readFor.ShouldEqual(typeof(string));
}
