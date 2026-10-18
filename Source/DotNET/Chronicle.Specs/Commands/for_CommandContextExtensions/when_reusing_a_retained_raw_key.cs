// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Keys;
using Cratis.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Chronicle.Commands.for_CommandContextExtensions;

public class when_reusing_a_retained_raw_key : Specification
{
    CommandWithCountedKey _command;
    CommandContext _context;
    object? _raw;

    void Establish()
    {
        _command = new();
        var values = new EventSourceValuesProvider(NullLogger<EventSourceValuesProvider>.Instance).Provide(_command);
        _context = new(CorrelationId.NotSet, _command.GetType(), _command, [], values);
    }

    void Because() => _raw = _context.GetRawEventSourceKey();
    [Fact] void should_preserve_the_original_object() => _raw.ShouldBeSame(_command.Original);
    [Fact] void should_not_read_the_command_property_again() => _command.Reads.ShouldEqual(1);

    public class CommandWithCountedKey
    {
        public object Original { get; } = new Composite("*");
        public int Reads { get; set; }
        [Key]
        public object Id
        {
            get
            {
                Reads++;
                return Original;
            }
        }
    }

    record Composite(string Part)
    {
        public override string ToString() => "target";
    }
}
