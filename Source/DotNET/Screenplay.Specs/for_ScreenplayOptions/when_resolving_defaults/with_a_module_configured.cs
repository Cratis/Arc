// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayOptions.when_resolving_defaults;

public class with_a_module_configured : Specification
{
    ScreenplayOptions _result;

    void Because() => _result = new ScreenplayOptions { Module = "Lending", SegmentsToSkip = 2 }.WithDefaults("Library");

    [Fact] void should_keep_the_configured_module() => _result.Module.ShouldEqual("Lending");
    [Fact] void should_keep_the_configured_segments_to_skip() => _result.SegmentsToSkip.ShouldEqual(2);
    [Fact] void should_still_name_the_domain_after_the_assembly() => _result.Domain.ShouldEqual("Library");
}
