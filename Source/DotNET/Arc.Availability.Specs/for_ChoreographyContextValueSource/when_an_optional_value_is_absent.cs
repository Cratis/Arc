// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_ChoreographyContextValueSource;

public class when_an_optional_value_is_absent : given.a_value_host
{
    Contexts.ContextValueRead _read;
    object _dynamicValue;

    async Task Because()
    {
        var values = ValuesFor(null);
        _read = await values.ReadAsync("owner");
        _dynamicValue = ((dynamic)values).Owner;
    }

    [Fact] void should_not_be_present() => _read.IsPresent.ShouldBeFalse();
    [Fact] void should_read_as_null() => _dynamicValue.ShouldBeNull();
}
