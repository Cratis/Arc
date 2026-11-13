// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Contexts.for_ContextValues;

public class when_a_command_context_has_no_services : Specification
{
    CommandContext _context;
    object _value;

    void Because()
    {
        _context = new CommandContext(CorrelationId.New(), typeof(string), "command", [], new());
        _value = ((dynamic)_context.ProvidedValues).Vessel;
    }

    [Fact] void should_read_the_value_as_absent() => _value.ShouldBeNull();
    [Fact] void should_keep_the_values_of_the_pipeline() => _context.Values.ShouldBeEmpty();
}
