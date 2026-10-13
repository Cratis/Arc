// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.given;

public class a_shared_command_scenario : an_observed_account
{
    protected readonly CommandScenario<ChangeObservedAccount> _commands = new();

    protected void ShareServices()
    {
        foreach (var resource in _scenario.Context.Values.Distinct().OfType<IDisposable>())
        {
            resource.Dispose();
        }
        _scenario.Context.Clear();
        _scenario.Services.Clear();
        foreach (var service in _commands.Services)
        {
            _scenario.Services.Add(service);
        }
        foreach (var (key, value) in _commands.Context)
        {
            _scenario.Context[key] = value;
        }
    }

    void Destroy() => _commands.Dispose();
}
