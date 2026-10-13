// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_a_passive_read_model : Specification
{
    readonly QueryScenario<PassiveObservedAccount> _scenario = new();
    QueryObservation _observation = default!;
    Exception? _error;

    async Task Because()
    {
        await using var services = _scenario.Services.BuildServiceProvider();
        _error = Catch.Exception(() => services.GetRequiredService<IReadModels>().Observe<PassiveObservedAccount>());
        _observation = await _scenario.Observe(nameof(PassiveObservedAccount.All));
    }

    [Fact] void should_preserve_chronicles_exception() => _error.ShouldBeOfExactType<ReadModelCannotBeObserved>();
    [Fact] void should_surface_the_error_through_the_query() => _observation.Initial.ExceptionMessages.ShouldContain(_error!.Message);

    void Destroy() => _scenario.Dispose();
}
