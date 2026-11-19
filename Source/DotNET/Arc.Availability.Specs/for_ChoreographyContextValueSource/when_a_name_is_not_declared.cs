// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_ChoreographyContextValueSource;

public class when_a_name_is_not_declared : given.a_value_host
{
    Contexts.ContextValueRead _read;

    async Task Because() => _read = await ValuesFor(null).ReadAsync("unknown");

    [Fact] void should_be_absent() => _read.IsPresent.ShouldBeFalse();
}
