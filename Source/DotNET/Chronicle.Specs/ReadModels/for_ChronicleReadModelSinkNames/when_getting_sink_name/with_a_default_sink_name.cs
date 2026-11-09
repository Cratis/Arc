// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleReadModelSinkNames.when_getting_sink_name;

public class with_a_default_sink_name : Specification
{
    ChronicleReadModelSinkNames _names;
    string? _declared;
    string? _registered;
    string? _passive;
    string? _unregistered;

    void Establish() => _names = new([typeof(given.RegisteredReadModel), typeof(given.ReadModelInNamedSink), typeof(given.PassiveReadModel)], "documents");

    void Because()
    {
        _declared = _names.GetSinkNameFor(typeof(given.ReadModelInNamedSink));
        _registered = _names.GetSinkNameFor(typeof(given.RegisteredReadModel));
        _passive = _names.GetSinkNameFor(typeof(given.PassiveReadModel));
        _unregistered = _names.GetSinkNameFor(typeof(given.UnregisteredDocument));
    }

    [Fact] void should_give_the_sink_a_read_model_declares() => _declared.ShouldEqual("reporting");
    [Fact] void should_give_the_default_sink_for_a_registered_read_model() => _registered.ShouldEqual("documents");
    [Fact] void should_give_no_sink_for_a_passive_read_model() => _passive.ShouldBeNull();
    [Fact] void should_give_no_sink_for_a_type_the_client_does_not_register() => _unregistered.ShouldBeNull();
}
