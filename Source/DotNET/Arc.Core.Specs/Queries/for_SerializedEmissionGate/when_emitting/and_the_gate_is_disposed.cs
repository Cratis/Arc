// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_SerializedEmissionGate.when_emitting;

/// <summary>
/// The subscription tears the gate down before the underlying subject has stopped delivering. The emission that
/// slips through must be dropped rather than throwing, because it runs from an async-void observer whose escaping
/// exception would crash the process.
/// </summary>
public class and_the_gate_is_disposed : Specification
{
    SerializedEmissionGate _gate;
    bool _ran;
    Exception _error;

    void Establish()
    {
        _gate = new SerializedEmissionGate();
        _gate.Dispose();
    }

    async Task Because() => _error = await Catch.Exception(Emit);

    Task Emit() => _gate.Emit(Run, CancellationToken.None);

    Task Run()
    {
        _ran = true;
        return Task.CompletedTask;
    }

    [Fact] void should_not_run_the_emission() => _ran.ShouldBeFalse();
    [Fact] void should_not_throw() => _error.ShouldBeNull();
}
