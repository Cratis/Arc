// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_appending_concurrently;

public class with_custom_execution_scopes : given.a_concurrent_append_scenario
{
    Exception _error;

    void Establish() => _scenario.Services.AddSingleton(Substitute.For<ICommandExecutionScope>());
    void Because() => _error = Catch.Exception(() => _scenario.AppendConcurrently(_source, new FactRecorded("competitor")));

    [Fact] void should_refuse_ambiguous_owner_ordering() => _error.ShouldBeOfExactType<ConcurrentAppendsCannotOrderCustomExecutionScopes>();
}
