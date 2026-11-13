// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_ChoreographyContextValueSource;

public class when_a_value_is_read_twice_in_one_request : given.a_value_host
{
    object _first;
    object _second;

    async Task Because()
    {
        var values = ValuesFor(typeof(given.an_availability_host.PlusCommand));
        _first = await values.GetAsync("vessel");
        _second = await ValuesFor(typeof(given.an_availability_host.PlusCommand)).GetAsync("vessel");
    }

    [Fact] void should_resolve_the_provider_once() => _calls.ShouldEqual(1);
    [Fact] void should_give_the_same_value_both_times() => _second.ShouldEqual(_first);
}
