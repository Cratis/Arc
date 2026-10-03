// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.given;
using Cratis.Arc.Screenplay.Embedded.Hosting;
using Microsoft.Extensions.Hosting;

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
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        _debugBuild = EventModelViewerExposure.ShouldExpose(options, an_application_assembly.BuiltForDebugging(), environment);
        _releaseBuild = EventModelViewerExposure.ShouldExpose(options, an_application_assembly.BuiltForRelease(), environment);
        _undeclaredBuild = EventModelViewerExposure.ShouldExpose(options, an_application_assembly.WithoutADebuggableDeclaration(), environment);
        _withoutAnEntryAssembly = EventModelViewerExposure.ShouldExpose(options, null, environment);
    }

    [Fact] void should_expose_the_explorer_for_a_debug_build() => _debugBuild.ShouldBeTrue();
    [Fact] void should_not_expose_the_explorer_for_a_release_build() => _releaseBuild.ShouldBeFalse();
    [Fact] void should_not_expose_the_explorer_for_a_build_that_declares_nothing() => _undeclaredBuild.ShouldBeFalse();
    [Fact] void should_not_expose_the_explorer_without_an_entry_assembly() => _withoutAnEntryAssembly.ShouldBeFalse();
}
