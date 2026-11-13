// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Contexts.for_ContextValues;

public class when_reading_from_a_command_context : given.a_provided_vessel
{
    object _value;
    VesselValues _typed;

    void Because()
    {
        var context = new CommandContext(CorrelationId.New(), typeof(string), "command", [], new()) { ServiceProvider = _services };
        _value = ((dynamic)context.ProvidedValues).Vessel;
        _typed = context.ValuesAs<VesselValues>();
    }

    [Fact] void should_read_the_provided_value() => _value.ShouldEqual("Aurora");
    [Fact] void should_read_for_the_command_type() => _readFor.ShouldEqual(typeof(string));
    [Fact] void should_read_the_values_as_a_type() => _typed.Vessel.ShouldEqual("Aurora");
}
