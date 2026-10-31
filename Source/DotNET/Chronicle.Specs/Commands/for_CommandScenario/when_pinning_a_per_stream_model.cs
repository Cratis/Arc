// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_automatically_enabling_decision_reads;
using static Cratis.Arc.Chronicle.ReadModels.for_per_stream_models.given.a_monthly_account;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_pinning_a_per_stream_model : Specification
{
    CommandScenario<OffStreamRead> _scenario;
    Exception _failure;

    void Establish() => _scenario = new();

    void Because()
    {
        var source = new AccountId("per-stream-pin");
        _failure = Catch.Exception(() => _scenario.Given.ForEventSource(source).ReadModel(new MonthlyWithdrawals(source, 123)))!;
    }

    [Fact] void should_refuse_before_execution() => _failure.ShouldBeOfExactType<PinnedPerStreamReadModelIsNotSupported>();
    [Fact] void should_name_the_model() => Assert.Contains(nameof(MonthlyWithdrawals), _failure.Message);
    [Fact] void should_prescribe_routed_events() => Assert.Contains("Given.ForEventSource(...).OnRoute(...).Events(...)", _failure.Message);

    async Task Destroy() => await _scenario.DisposeAsync();
}
