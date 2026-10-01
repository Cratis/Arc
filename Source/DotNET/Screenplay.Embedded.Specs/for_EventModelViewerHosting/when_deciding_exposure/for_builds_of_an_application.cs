// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.given;
using Cratis.Arc.Screenplay.Embedded.Hosting;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.when_deciding_exposure;

public class for_builds_of_an_application : Specification
{
    bool _debugBuild;
    bool _releaseBuild;
    bool _undeclaredBuild;
    bool _withoutAnEntryAssembly;

    void Because()
    {
        var options = new EventModelViewerOptions();
        _debugBuild = EventModelViewerExposure.ShouldExpose(options, an_application_assembly.BuiltForDebugging());
        _releaseBuild = EventModelViewerExposure.ShouldExpose(options, an_application_assembly.BuiltForRelease());
        _undeclaredBuild = EventModelViewerExposure.ShouldExpose(options, an_application_assembly.WithoutADebuggableDeclaration());
        _withoutAnEntryAssembly = EventModelViewerExposure.ShouldExpose(options, null);
    }

    [Fact] void should_expose_the_explorer_for_a_debug_build() => _debugBuild.ShouldBeTrue();
    [Fact] void should_not_expose_the_explorer_for_a_release_build() => _releaseBuild.ShouldBeFalse();
    [Fact] void should_not_expose_the_explorer_for_a_build_that_declares_nothing() => _undeclaredBuild.ShouldBeFalse();
    [Fact] void should_not_expose_the_explorer_without_an_entry_assembly() => _withoutAnEntryAssembly.ShouldBeFalse();
}
