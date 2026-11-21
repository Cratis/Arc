// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario;

public class when_configuring_an_unauthenticated_caller : Specification
{
    readonly CommandScenario<InspectScenarioCaller> _scenario = new();
    CommandScenario<InspectScenarioCaller> _returnedScenario = default!;
    InspectScenarioCaller.Observation _observation = default!;

    void Establish() => _scenario.WithClaims(new Claim("sub", "previous-caller"));

    async Task Because()
    {
        _returnedScenario = _scenario.Unauthenticated();
        var result = await _scenario.Execute(new InspectScenarioCaller());
        _observation = ((CommandResult<InspectScenarioCaller.Observation>)result).Response;
    }

    [Fact] void should_return_the_scenario() => _returnedScenario.ShouldEqual(_scenario);
    [Fact] void should_not_authenticate_the_caller() => _observation.Principal!.Identity!.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_have_no_claims() => _observation.Principal!.Claims.ShouldBeEmpty();
    [Fact] void should_resolve_a_missing_caller_subject() => _observation.Subject.ShouldEqual(ReadModelKeyLookup.Missing);

    void Destroy() => _scenario.Dispose();
}
