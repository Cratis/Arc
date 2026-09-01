// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_CommandScenario.when_disposing;

/// <summary>
/// Disposing an executed scenario must dispose the service provider it built, releasing every
/// container-owned disposable the pipeline materialized.
/// </summary>
public class and_the_scenario_has_executed : Specification
{
    CommandScenario<PerformWork> _scenario;
    TrackedResource _resource;
    CommandResult _result;

    async Task Establish()
    {
        _resource = new TrackedResource();
        _scenario = new CommandScenario<PerformWork>();
        _scenario.Services.AddSingleton(_ => _resource);
        _result = await _scenario.Execute(new PerformWork());
    }

    void Because() => _scenario.Dispose();

    [Fact] void should_execute_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_materialized_the_resource() => _resource.Touched.ShouldBeTrue();
    [Fact] void should_dispose_the_service_provider_owned_resource() => _resource.DisposeCount.ShouldEqual(1);
}
