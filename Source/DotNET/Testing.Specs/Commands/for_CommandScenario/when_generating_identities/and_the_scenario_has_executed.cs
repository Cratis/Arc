// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario.when_generating_identities;

public class and_the_scenario_has_executed : Specification
{
    readonly CommandScenario<GenerateIdentity> _scenario = new();
    readonly Guid _id = Guid.NewGuid();
    CommandResult _result;

    async Task Establish() => (await _scenario.Execute(new GenerateIdentity())).ShouldBeSuccessful();

    async Task Because()
    {
        _scenario.Generate(_id);
        _result = await _scenario.Execute(new GenerateIdentity());
    }

    [Fact] void should_use_the_newly_queued_identity() => ((CommandResult<Guid>)_result).Response.ShouldEqual(_id);

    void Destroy() => _scenario.Dispose();
}
