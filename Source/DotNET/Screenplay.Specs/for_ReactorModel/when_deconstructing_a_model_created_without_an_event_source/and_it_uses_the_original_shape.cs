// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ReactorModel.when_deconstructing_a_model_created_without_an_event_source;

/// <summary>
/// A consumer built against the original model shape keeps constructing and deconstructing it.
/// </summary>
public class and_it_uses_the_original_shape : Specification
{
    ReactorModel _model;
    string _name;
    bool _translating;
    string _path;

    void Establish() => _model = new("Notifier", ["FundsDeposited"], true, "a.cs");

    void Because() => (_name, _, _translating, _path) = _model;

    [Fact] void should_have_no_event_source() => _model.EventSource.ShouldBeNull();
    [Fact] void should_deconstruct_the_name() => _name.ShouldEqual("Notifier");
    [Fact] void should_deconstruct_translating() => _translating.ShouldBeTrue();
    [Fact] void should_deconstruct_the_path() => _path.ShouldEqual("a.cs");
}
