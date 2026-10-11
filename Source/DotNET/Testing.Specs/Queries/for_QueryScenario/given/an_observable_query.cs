// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.given;

public class an_observable_query : Specification
{
    protected readonly QueryScenario<ObservableScenarioReadModel> _scenario = new();
    protected readonly ObservableScenarioState _state = new();

    void Establish() => _scenario.Services.AddSingleton(_state);

    async Task Destroy()
    {
        await _scenario.DisposeAsync();
        (_state.Subject as IDisposable)?.Dispose();
        _state.Collection.Dispose();
    }
}
