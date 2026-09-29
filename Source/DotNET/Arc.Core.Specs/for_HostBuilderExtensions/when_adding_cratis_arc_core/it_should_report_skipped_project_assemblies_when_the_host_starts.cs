// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

public class it_should_report_skipped_project_assemblies_when_the_host_starts : Specification
{
    IServiceCollection _services;

    void Because()
    {
        _services = new ServiceCollection();
        _services.AddOptions();
        _services.AddCratisArcCore();
        _services.AddCratisArcCore();
    }

    [Fact] void should_register_the_reporter_once() => _services.Count(_ => _.ServiceType == typeof(IHostedService) && _.ImplementationType == typeof(SkippedProjectAssembliesReporter)).ShouldEqual(1);
}
