// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario;

public class when_one_of_two_scenarios_configures_claims : Specification
{
    readonly CommandScenario<InspectScenarioCaller> _configured = new();
    readonly CommandScenario<InspectScenarioCaller> _unconfigured = new();
    InspectScenarioCaller.Observation _configuredCaller = default!;
    InspectScenarioCaller.Observation _unconfiguredCaller = default!;

    void Establish() => _configured.WithClaims(new Claim("sub", "isolated-caller"));

    async Task Because()
    {
        var configured = await _configured.Execute(new InspectScenarioCaller());
        var unconfigured = await _unconfigured.Execute(new InspectScenarioCaller());
        _configuredCaller = ((CommandResult<InspectScenarioCaller.Observation>)configured).Response;
        _unconfiguredCaller = ((CommandResult<InspectScenarioCaller.Observation>)unconfigured).Response;
    }

    [Fact] void should_use_the_configured_subject() => _configuredCaller.Subject.ShouldEqual(ReadModelKeyLookup.Found("isolated-caller"));
    [Fact] void should_leave_the_unconfigured_scenario_without_a_principal() => _unconfiguredCaller.Principal.ShouldBeNull();
    [Fact] void should_not_leak_the_subject() => _unconfiguredCaller.Subject.ShouldEqual(ReadModelKeyLookup.Missing);

    void Destroy()
    {
        _configured.Dispose();
        _unconfigured.Dispose();
    }
}
