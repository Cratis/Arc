// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_SerializedEmissionGate.when_emitting;

public class and_the_token_is_cancelled : Specification
{
    SerializedEmissionGate _gate;
    bool _ran;
    Exception _error;

    void Establish() => _gate = new SerializedEmissionGate();

    async Task Because() => _error = await Catch.Exception(Emit);

    Task Emit() => _gate.Emit(Run, new CancellationToken(canceled: true));

    Task Run()
    {
        _ran = true;
        return Task.CompletedTask;
    }

    [Fact] void should_not_run_the_emission() => _ran.ShouldBeFalse();
    [Fact] void should_not_throw() => _error.ShouldBeNull();

    void Destroy() => _gate.Dispose();
}
