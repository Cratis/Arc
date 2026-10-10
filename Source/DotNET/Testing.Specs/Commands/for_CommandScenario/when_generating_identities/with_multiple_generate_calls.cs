// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario.when_generating_identities;

public class with_multiple_generate_calls : Specification
{
    readonly CommandScenario<GenerateIdentity> _scenario = new();
    readonly Guid _first = Guid.NewGuid();
    readonly Guid _second = Guid.NewGuid();
    CommandResult _firstResult;
    CommandResult _secondResult;
    CommandResult _fallbackResult;

    void Establish()
    {
        _scenario.Generate(_first);
        _scenario.Generate(_second);
    }

    async Task Because()
    {
        _firstResult = await _scenario.Execute(new GenerateIdentity());
        _secondResult = await _scenario.Execute(new GenerateIdentity());
        _fallbackResult = await _scenario.Execute(new GenerateIdentity());
    }

    [Fact] void should_succeed() => _firstResult.ShouldBeSuccessful();
    [Fact] void should_consume_the_first_identity() => ((CommandResult<Guid>)_firstResult).Response.ShouldEqual(_first);
    [Fact] void should_append_the_second_identity() => ((CommandResult<Guid>)_secondResult).Response.ShouldEqual(_second);
    [Fact] void should_fall_back_to_a_new_identity() => ((CommandResult<Guid>)_fallbackResult).Response.ShouldNotEqual(Guid.Empty);
    [Fact] void should_not_reuse_the_last_identity() => ((CommandResult<Guid>)_fallbackResult).Response.ShouldNotEqual(_second);

    void Destroy() => _scenario.Dispose();
}
