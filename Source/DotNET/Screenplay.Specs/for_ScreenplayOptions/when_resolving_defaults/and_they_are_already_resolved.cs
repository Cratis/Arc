// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayOptions.when_resolving_defaults;

/// <summary>
/// A generation resolves for the analysis half against the assembly it is reading, and the emission half resolves
/// against the domain of the model it is handed. Both run on the way through one generation, so resolving what is
/// already resolved has to answer the same thing however a second fallback would have answered - otherwise which
/// entry point was used decides what the document is called.
/// </summary>
public class and_they_are_already_resolved : Specification
{
    ScreenplayOptions _resolved;
    ScreenplayOptions _resolvedAgain;

    void Because()
    {
        _resolved = new ScreenplayOptions().WithDefaults("Library");
        _resolvedAgain = _resolved.WithDefaults("SomethingElse");
    }

    [Fact] void should_answer_the_same_thing_a_second_time() => _resolvedAgain.ShouldEqual(_resolved);
    [Fact] void should_not_take_the_second_fallback_for_the_domain() => _resolvedAgain.Domain.ShouldEqual("Library");
    [Fact] void should_not_take_the_second_fallback_for_the_module() => _resolvedAgain.Module.ShouldEqual("Library");
}
