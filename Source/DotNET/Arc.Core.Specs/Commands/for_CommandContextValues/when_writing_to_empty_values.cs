// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandContextValues;

public class when_writing_to_empty_values : Specification
{
    CommandContextValues _written = null!;
    CommandContextValues _other = null!;

    void Because()
    {
        _written = CommandContextValues.Empty;
        _written["cached"] = "value";
        _other = CommandContextValues.Empty;
    }

    [Fact] void should_return_a_new_instance_on_each_access() => _other.ShouldNotBeSame(_written);
    [Fact] void should_not_leak_the_written_value_into_other_empty_values() => _other.ShouldBeEmpty();
}
