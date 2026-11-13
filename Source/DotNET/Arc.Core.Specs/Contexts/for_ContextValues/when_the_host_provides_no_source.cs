// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Contexts.for_ContextValues;

public class when_the_host_provides_no_source : Specification
{
    ContextValues _values;
    object _value;

    void Because()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        _values = ContextValues.For(services, () => null);
        _value = _values.AsDynamic().Vessel;
    }

    [Fact] void should_not_be_provided() => _values.IsProvided.ShouldBeFalse();
    [Fact] void should_read_every_value_as_absent() => _value.ShouldBeNull();
}
