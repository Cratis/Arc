// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario.when_generating_identities;

public class with_independent_scenarios : Specification
{
    readonly CommandScenario<GenerateIdentity> _first = new();
    readonly CommandScenario<GenerateIdentity> _second = new();
    readonly Guid _firstId = Guid.NewGuid();
    readonly Guid _secondId = Guid.NewGuid();
    CommandResult _firstResult;
    CommandResult _secondResult;

    void Establish()
    {
        _first.Generate(_firstId);
        _second.Generate(_secondId);
    }

    async Task Because()
    {
        _secondResult = await _second.Execute(new GenerateIdentity());
        _firstResult = await _first.Execute(new GenerateIdentity());
    }

    [Fact] void should_keep_the_first_queue_local() => ((CommandResult<Guid>)_firstResult).Response.ShouldEqual(_firstId);
    [Fact] void should_keep_the_second_queue_local() => ((CommandResult<Guid>)_secondResult).Response.ShouldEqual(_secondId);

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
    }
}
