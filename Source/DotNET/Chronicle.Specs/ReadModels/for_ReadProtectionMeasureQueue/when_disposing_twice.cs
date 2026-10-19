// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasureQueue;

public class when_disposing_twice : Specification
{
    ReadProtectionMeasureQueue _queue;
    Exception _error;

    async Task Establish()
    {
        _queue = new();
        await _queue.StartAsync(CancellationToken.None);
        await _queue.StopAsync(CancellationToken.None);
        _queue.Dispose();
    }

    void Because() => _error = Catch.Exception(_queue.Dispose);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
}
