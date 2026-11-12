// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Keys;
using Cratis.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_the_mode_is_off_without_generated_metadata : Specification
{
    string _key;

    void Because()
    {
        var command = new Lookup("abc");
        var values = new EventSourceValuesProvider(NullLogger<EventSourceValuesProvider>.Instance).Provide(command);
        var context = new CommandContext(CorrelationId.New(), typeof(Lookup), command, [], values);
        var parameter = typeof(Lookup).GetMethod(nameof(Lookup.Handle))!.GetParameters()[0];

        _key = CommandReadModelLookup.Resolve(context, parameter, ReadProtectionMode.Off, ProtectedKeyTraversalForSpecs.Create(), out _).Value;
    }

    [Fact] void should_resolve_the_key_without_requiring_generated_metadata() => _key.ShouldEqual("abc");

    public record Lookup([property: Key] string Source)
    {
        public void Handle(Model model) { }
    }

    public record Model(string Id);
}
