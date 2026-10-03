// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.given;
using Cratis.Arc.Screenplay.Embedded.Hosting;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.when_deciding_exposure;

public class for_hosting_environments : Specification
{
    bool _development;
    bool _production;
    bool _staging;
    bool _unknown;
    bool _withoutAnEnvironment;
    bool _enabledInProduction;
    bool _enabledInStaging;

    void Because()
    {
        var assembly = an_application_assembly.BuiltForDebugging();
        var options = new EventModelViewerOptions();
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        _development = EventModelViewerExposure.ShouldExpose(options, assembly, environment);
        environment.EnvironmentName.Returns(Environments.Production);
        _production = EventModelViewerExposure.ShouldExpose(options, assembly, environment);
        _enabledInProduction = EventModelViewerExposure.ShouldExpose(new EventModelViewerOptions { Enabled = true }, assembly, environment);
        environment.EnvironmentName.Returns(Environments.Staging);
        _staging = EventModelViewerExposure.ShouldExpose(options, assembly, environment);
        _enabledInStaging = EventModelViewerExposure.ShouldExpose(new EventModelViewerOptions { Enabled = true }, assembly, environment);
        environment.EnvironmentName.Returns("Custom");
        _unknown = EventModelViewerExposure.ShouldExpose(options, assembly, environment);
        _withoutAnEnvironment = EventModelViewerExposure.ShouldExpose(options, assembly, null);
    }

    [Fact] void should_expose_a_debug_build_in_development() => _development.ShouldBeTrue();
    [Fact] void should_not_expose_a_debug_build_in_production_by_default() => _production.ShouldBeFalse();
    [Fact] void should_not_expose_a_debug_build_in_staging_by_default() => _staging.ShouldBeFalse();
    [Fact] void should_not_expose_a_debug_build_in_an_unknown_environment_by_default() => _unknown.ShouldBeFalse();
    [Fact] void should_not_expose_a_debug_build_without_a_hosting_environment_by_default() => _withoutAnEnvironment.ShouldBeFalse();
    [Fact] void should_honor_explicit_enablement_in_production() => _enabledInProduction.ShouldBeTrue();
    [Fact] void should_honor_explicit_enablement_in_staging() => _enabledInStaging.ShouldBeTrue();
}
