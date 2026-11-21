// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario;

public class when_configuring_a_principal : Specification
{
    readonly CommandScenario<InspectScenarioCaller> _scenario = new();
    readonly ClaimsPrincipal _principal = new(new ClaimsIdentity([new Claim("sub", "caller-123")], "test"));
    CommandScenario<InspectScenarioCaller> _returnedScenario = default!;
    InspectScenarioCaller.Observation _observation = default!;

    async Task Because()
    {
        _returnedScenario = _scenario.WithPrincipal(_principal);
        var result = await _scenario.Execute(new InspectScenarioCaller());
        _observation = ((CommandResult<InspectScenarioCaller.Observation>)result).Response;
    }

    [Fact] void should_return_the_scenario() => _returnedScenario.ShouldEqual(_scenario);
    [Fact] void should_use_the_principal_during_execution() => _observation.Principal.ShouldEqual(_principal);
    [Fact] void should_resolve_the_caller_subject() => _observation.Subject.ShouldEqual(ReadModelKeyLookup.Found("caller-123"));

    void Destroy() => _scenario.Dispose();
}
