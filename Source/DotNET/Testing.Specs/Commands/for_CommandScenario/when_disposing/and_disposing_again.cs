// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario.when_disposing;

/// <summary>
/// Disposal is idempotent: a second dispose neither throws nor disposes anything a second time.
/// </summary>
public class and_disposing_again : Specification
{
    CommandScenario<PerformWork> _scenario;
    TrackedResource _resource;
    Exception _error;

    void Establish()
    {
        _scenario = new CommandScenario<PerformWork>();
        _resource = new TrackedResource();
        _scenario.Context["resource"] = _resource;
    }

    void Because()
    {
        _scenario.Dispose();
        _error = Catch.Exception(_scenario.Dispose);
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_only_dispose_once() => _resource.DisposeCount.ShouldEqual(1);
}
