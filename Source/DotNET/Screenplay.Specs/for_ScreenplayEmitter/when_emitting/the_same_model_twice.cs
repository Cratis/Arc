// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Library;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

/// <summary>
/// A generated document is only worth committing if regenerating it produces the same bytes. The two models are
/// built separately on purpose - equal content arriving as different instances, in whatever order the collections
/// happen to enumerate in, has to reach the printer arranged identically.
/// </summary>
public class the_same_model_twice : given.an_emitter
{
    string _first;
    string _second;

    void Because()
    {
        _first = _emitter.Emit(LibraryApplication.Build(), _options).Source;
        _second = _emitter.Emit(LibraryApplication.Build(), _options).Source;
    }

    [Fact] void should_produce_identical_output() => _second.ShouldEqual(_first);
    [Fact] void should_produce_output_at_all() => _first.ShouldNotBeEmpty();
}
