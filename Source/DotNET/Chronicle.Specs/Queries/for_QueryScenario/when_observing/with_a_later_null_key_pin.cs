// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_a_later_null_key_pin : given.an_observed_account
{
    QueryResult _next = default!;

    async Task Establish() => _observation = await _scenario.Observe(nameof(ObservedAccount.All));

    async Task Because()
    {
        _scenario.Given.ForEventSource(_id).ReadModel(new ObservedAccount(null!, 42m));
        _next = await _observation.Next();
    }

    [Fact] void should_fail_the_observation() => _next.HasExceptions.ShouldBeTrue();
    [Fact] void should_report_the_pin_key_failure() => _next.ExceptionMessages.ShouldContain(new PinnedReadModelCannotBeObserved(typeof(ObservedAccount)).Message);
}
