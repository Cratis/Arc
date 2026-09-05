// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario.when_disposing;

/// <summary>
/// Extenders park resources in the context dictionary; disposing the scenario must dispose them —
/// even when the scenario was never initialized — so extension packages need no coupling to be cleaned up.
/// </summary>
public class and_the_context_holds_disposable_values : Specification
{
    CommandScenario<PerformWork> _scenario;
    TrackedResource _resource;

    void Establish()
    {
        _scenario = new CommandScenario<PerformWork>();
        _resource = new TrackedResource();
        _scenario.Context["resource"] = _resource;
    }

    void Because() => _scenario.Dispose();

    [Fact] void should_dispose_the_context_value() => _resource.DisposeCount.ShouldEqual(1);
}
