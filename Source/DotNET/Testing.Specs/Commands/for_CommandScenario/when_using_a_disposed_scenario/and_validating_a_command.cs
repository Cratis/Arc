// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario.when_using_a_disposed_scenario;

/// <summary>
/// Validation goes through the same guard as execution: a disposed scenario fails loudly.
/// </summary>
public class and_validating_a_command : Specification
{
    CommandScenario<PerformWork> _scenario;
    Exception _error;

    void Establish()
    {
        _scenario = new CommandScenario<PerformWork>();
        _scenario.Dispose();
    }

    async Task Because() => _error = await Catch.Exception(() => _scenario.Validate(new PerformWork()));

    [Fact] void should_throw_object_disposed() => _error.ShouldBeOfExactType<ObjectDisposedException>();
}
