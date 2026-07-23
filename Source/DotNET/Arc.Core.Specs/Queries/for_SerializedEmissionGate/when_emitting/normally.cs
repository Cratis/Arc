// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_SerializedEmissionGate.when_emitting;

public class normally : Specification
{
    SerializedEmissionGate _gate;
    bool _ran;

    void Establish() => _gate = new SerializedEmissionGate();

    async Task Because() => await _gate.Emit(Run, CancellationToken.None);

    Task Run()
    {
        _ran = true;
        return Task.CompletedTask;
    }

    [Fact] void should_run_the_emission() => _ran.ShouldBeTrue();

    void Destroy() => _gate.Dispose();
}
