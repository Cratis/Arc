// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario;

public class when_two_scenarios_run_in_one_process : Specification
{
    readonly CommandScenario<ReportSingletons> _first = new();
    readonly CommandScenario<ReportSingletons> _second = new();
    ResolvedSingletons _fromTheFirst = default!;
    ResolvedSingletons _fromTheSecond = default!;

    async Task Because()
    {
        _fromTheFirst = ((CommandResult<ResolvedSingletons>)await _first.Execute(new ReportSingletons())).Response!;
        _fromTheSecond = ((CommandResult<ResolvedSingletons>)await _second.Execute(new ReportSingletons())).Response!;
    }

    [Fact] void should_give_each_scenario_its_own_principal_accessor() => ReferenceEquals(_fromTheFirst.PrincipalAccessor, _fromTheSecond.PrincipalAccessor).ShouldBeFalse();
    [Fact] void should_give_each_scenario_its_own_policy_runtime() => ReferenceEquals(_fromTheFirst.PolicyRuntime, _fromTheSecond.PolicyRuntime).ShouldBeFalse();

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
    }
}
