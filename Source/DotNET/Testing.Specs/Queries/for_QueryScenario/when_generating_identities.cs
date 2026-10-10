// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_generating_identities : Specification
{
    readonly QueryScenario<GeneratedIdentity> _scenario = new();
    readonly Guid _first = Guid.NewGuid();
    readonly Guid _second = Guid.NewGuid();
    QueryResult _firstResult;
    QueryResult _secondResult;
    QueryResult _fallbackResult;

    void Establish() => _scenario.Generate(_first);

    async Task Because()
    {
        _firstResult = await _scenario.Perform(nameof(GeneratedIdentity.Generate));
        _scenario.Generate(_second);
        _secondResult = await _scenario.Perform(nameof(GeneratedIdentity.Generate));
        _fallbackResult = await _scenario.Perform(nameof(GeneratedIdentity.Generate));
    }

    [Fact] void should_succeed() => Assert.True(_firstResult.IsSuccess, string.Join("; ", _firstResult.ExceptionMessages));
    [Fact] void should_consume_the_queued_identity() => ((GeneratedIdentity)_firstResult.Data).Value.ShouldEqual(_first);
    [Fact] void should_accept_identities_after_a_query() => ((GeneratedIdentity)_secondResult.Data).Value.ShouldEqual(_second);
    [Fact] void should_generate_an_identity_after_exhaustion() => ((GeneratedIdentity)_fallbackResult.Data).Value.ShouldNotEqual(Guid.Empty);
    [Fact] void should_not_reuse_the_last_identity() => ((GeneratedIdentity)_fallbackResult.Data).Value.ShouldNotEqual(_second);

    void Destroy() => _scenario.Dispose();
}
