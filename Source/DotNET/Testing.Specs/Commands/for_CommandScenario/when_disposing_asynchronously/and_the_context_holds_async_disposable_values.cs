// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario.when_disposing_asynchronously;

/// <summary>
/// Asynchronous disposal prefers <see cref="IAsyncDisposable"/> on context values, only falling back
/// to <see cref="IDisposable"/> when a value has no asynchronous path.
/// </summary>
public class and_the_context_holds_async_disposable_values : Specification
{
    CommandScenario<PerformWork> _scenario;
    AsyncTrackedResource _asyncResource;
    TrackedResource _syncOnlyResource;

    void Establish()
    {
        _scenario = new CommandScenario<PerformWork>();
        _asyncResource = new AsyncTrackedResource();
        _syncOnlyResource = new TrackedResource();
        _scenario.Context["async"] = _asyncResource;
        _scenario.Context["sync"] = _syncOnlyResource;
    }

    async Task Because() => await _scenario.DisposeAsync();

    [Fact] void should_dispose_through_the_async_path() => _asyncResource.AsyncDisposeCount.ShouldEqual(1);
    [Fact] void should_not_dispose_through_the_sync_path() => _asyncResource.DisposeCount.ShouldEqual(0);
    [Fact] void should_fall_back_to_sync_disposal_for_sync_only_values() => _syncOnlyResource.DisposeCount.ShouldEqual(1);
}
