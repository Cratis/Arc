// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Sinks.for_ReadModelSinks.when_getting_sink_name;

public class for_a_type_without_a_sink : given.read_model_sinks
{
    string? _withoutDeclaration;
    string? _withBlankDeclaration;

    void Because()
    {
        _withoutDeclaration = _sinks.GetSinkNameFor(typeof(given.ReadModelInDefaultSink));
        _withBlankDeclaration = _sinks.GetSinkNameFor(typeof(given.ReadModelWithBlankSink));
    }

    [Fact] void should_give_no_name_for_a_type_nothing_names_a_sink_for() => _withoutDeclaration.ShouldBeNull();
    [Fact] void should_treat_a_blank_declared_name_as_the_default_sink() => _withBlankDeclaration.ShouldBeNull();
}
