// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_a_pin_without_a_readable_key : Specification
{
    readonly QueryScenario<ObservedBalance> _scenario = new();
    Exception? _error;
    QueryObservation _observation = default!;

    void Establish() => _scenario.Given.ForEventSource(EventSourceId.New()).ReadModel(new ObservedBalance(42m));

    async Task Because()
    {
        await using var services = _scenario.Services.BuildServiceProvider();
        _error = Catch.Exception(() => services.GetRequiredService<IReadModels>().Observe<ObservedBalance>());
        _observation = await _scenario.Observe(nameof(ObservedBalance.All));
    }

    [Fact] void should_refuse_an_ambiguous_pin() => _error.ShouldBeOfExactType<PinnedReadModelCannotBeObserved>();
    [Fact] void should_name_the_read_model() => _error!.Message.Contains(typeof(ObservedBalance).FullName!, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_explain_how_to_seed_instead() => _error!.Message.Contains("Seed events instead of pinning, or give the read model a key property", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_surface_the_error_through_the_query() => _observation.Initial.ExceptionMessages.ShouldContain(_error!.Message);

    void Destroy() => _scenario.Dispose();
}
