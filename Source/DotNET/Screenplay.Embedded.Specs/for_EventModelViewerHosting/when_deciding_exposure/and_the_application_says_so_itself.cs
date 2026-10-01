// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.given;
using Cratis.Arc.Screenplay.Embedded.Hosting;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.when_deciding_exposure;

public class and_the_application_says_so_itself : Specification
{
    bool _turnedOffInADebugBuild;
    bool _turnedOnInAReleaseBuild;

    void Because()
    {
        _turnedOffInADebugBuild = EventModelViewerExposure.ShouldExpose(
            new EventModelViewerOptions { Enabled = false },
            an_application_assembly.BuiltForDebugging());

        _turnedOnInAReleaseBuild = EventModelViewerExposure.ShouldExpose(
            new EventModelViewerOptions { Enabled = true },
            an_application_assembly.BuiltForRelease());
    }

    [Fact] void should_not_expose_the_explorer_the_application_turned_off() => _turnedOffInADebugBuild.ShouldBeFalse();
    [Fact] void should_expose_the_explorer_the_application_turned_on() => _turnedOnInAReleaseBuild.ShouldBeTrue();
}
