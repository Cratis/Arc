// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class with_an_unknown_read_model : Specification
{
    readonly QueryScenario<QueryAccountBalance> _scenario = new();
    Exception? _error;

    void Because()
    {
        using var services = _scenario.Services.BuildServiceProvider();
        _error = Catch.Exception(() => services.GetRequiredService<IReadModels>().Observe<QueryAccountBalance>());
    }

    [Fact] void should_preserve_chronicles_exception() => _error.ShouldBeOfExactType<UnknownReadModel>();

    void Destroy() => _scenario.Dispose();
}
