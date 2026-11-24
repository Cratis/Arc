// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_evaluating_an_explicit_context : given.an_evaluation
{
    readonly DateTimeOffset _receivedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    readonly object _dependency = new();
    CommandContext _context;
    CommandContext _observed;

    void Establish()
    {
        _context = new(CorrelationId.New(), typeof(given.ResourceCommand), new given.ResourceCommand(true), [_dependency], CommandContextValues.Empty, ServiceProvider: Provider) { ReceivedAt = _receivedAt };
        Behavior.Evaluate = (policy, _) =>
        {
            _observed = (CommandContext)policy.Resource;
            return Task.FromResult(policy.ReceivedAt == _receivedAt);
        };
    }

    async Task Because() => Result = await CommandAuthorization.ForContext<given.ResourceCommand>(Provider, Principal, _context);
    [Fact] void should_authorize() => Result.ShouldBeAuthorized();
    [Fact] void should_use_a_fresh_context() => ReferenceEquals(_observed, _context).ShouldBeFalse();
    [Fact] void should_preserve_dependencies() => _observed.Dependencies.Single().ShouldEqual(_dependency);
    [Fact] void should_preserve_values() => _observed.Values.ShouldEqual(_context.Values);
    [Fact] void should_not_publish_a_verdict_to_the_fixture() => _context.AuthorizedPrincipal.ShouldBeNull();
}
