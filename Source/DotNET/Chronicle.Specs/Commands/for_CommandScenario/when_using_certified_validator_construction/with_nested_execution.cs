// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction;

public class with_nested_execution : given.a_certified_command
{
    readonly List<CertifiedValidator> _seen = [];
    CommandResult _result;

    void Establish()
    {
        _command = _command with { Nested = EventSourceId.New() };
        _scenario.Services.AddProtectedDecisionValidator<CertifiedValidator>(_seen.Add);
    }

    async Task Because()
    {
        Build();
        _result = await _pipeline.Execute(_command);
    }

    [Fact] void should_accept_nested_providerless_scopes() => _result.ShouldBeSuccessful();
    [Fact] void should_construct_each_validator_for_its_own_invocation() => _seen.Count.ShouldEqual(2);
    [Fact] void should_issue_different_read_tokens() => _seen[0].Read.ShouldNotBeSame(_seen[1].Read);
    [Fact] void should_commit_both_owner_events() => _scenario.AppendedEvents.Count.ShouldEqual(2);
}
