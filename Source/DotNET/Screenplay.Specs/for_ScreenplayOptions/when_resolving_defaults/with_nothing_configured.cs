// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayOptions.when_resolving_defaults;

public class with_nothing_configured : Specification
{
    ScreenplayOptions _fromAName;
    ScreenplayOptions _fromNothing;

    void Because()
    {
        _fromAName = new ScreenplayOptions().WithDefaults("Library");
        _fromNothing = new ScreenplayOptions().WithDefaults(null);
    }

    [Fact] void should_name_the_domain_after_the_assembly() => _fromAName.Domain.ShouldEqual("Library");
    [Fact] void should_name_the_module_after_the_domain() => _fromAName.Module.ShouldEqual("Library");
    [Fact] void should_skip_no_segments() => _fromAName.SegmentsToSkip.ShouldEqual(0);
    [Fact] void should_fall_back_when_there_is_no_assembly_name() => _fromNothing.Domain.ShouldEqual(ScreenplayOptions.DefaultName);
}
