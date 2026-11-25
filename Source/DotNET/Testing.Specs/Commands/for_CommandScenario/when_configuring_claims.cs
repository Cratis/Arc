// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario;

public class when_configuring_claims : Specification
{
    readonly CommandScenario<InspectScenarioCaller> _scenario = new();
    readonly Claim[] _claims = [new("sub", "claimed-caller"), new("department", "sales")];
    CommandScenario<InspectScenarioCaller> _returnedScenario = default!;
    InspectScenarioCaller.Observation _observation = default!;

    async Task Because()
    {
        _returnedScenario = _scenario.WithClaims(_claims);
        var result = await _scenario.Execute(new InspectScenarioCaller());
        _observation = ((CommandResult<InspectScenarioCaller.Observation>)result).Response;
    }

    [Fact] void should_return_the_scenario() => _returnedScenario.ShouldEqual(_scenario);
    [Fact] void should_authenticate_the_caller() => _observation.Principal!.Identity!.IsAuthenticated.ShouldBeTrue();
    [Fact]
    void should_carry_the_supplied_claims() =>
        _observation.Principal!.Claims.Select(claim => (claim.Type, claim.Value)).ShouldEqual(_claims.Select(claim => (claim.Type, claim.Value)));
    [Fact] void should_resolve_the_caller_subject() => _observation.Subject.ShouldEqual(ReadModelKeyLookup.Found("claimed-caller"));

    void Destroy() => _scenario.Dispose();
}
