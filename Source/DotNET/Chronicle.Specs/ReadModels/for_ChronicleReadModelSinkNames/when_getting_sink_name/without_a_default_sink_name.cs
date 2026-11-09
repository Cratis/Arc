// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleReadModelSinkNames.when_getting_sink_name;

public class without_a_default_sink_name : Specification
{
    ChronicleReadModelSinkNames _names;
    string? _declared;
    string? _registered;

    void Establish() => _names = new([typeof(given.RegisteredReadModel), typeof(given.ReadModelInNamedSink)], string.Empty);

    void Because()
    {
        _declared = _names.GetSinkNameFor(typeof(given.ReadModelInNamedSink));
        _registered = _names.GetSinkNameFor(typeof(given.RegisteredReadModel));
    }

    [Fact] void should_give_the_sink_a_read_model_declares() => _declared.ShouldEqual("reporting");
    [Fact] void should_leave_a_registered_read_model_in_the_default_sink() => _registered.ShouldBeNull();
}
