// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_ChoreographyContextValueSource;

public class when_a_value_is_read_in_a_second_request : given.a_value_host
{
    async Task Because()
    {
        await ValuesFor(typeof(given.an_availability_host.PlusCommand)).GetAsync("vessel");
        await using var second = _root.CreateAsyncScope();
        await Cratis.Arc.Contexts.ContextValues.For(second.ServiceProvider, () => typeof(given.an_availability_host.PlusCommand)).GetAsync("vessel");
    }

    [Fact] void should_resolve_the_provider_for_each_request() => _calls.ShouldEqual(2);
}
