// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models;

public class when_plain_stream_state_reaches_a_handler : given.a_monthly_account
{
    [Theory]
    [InlineData("Off")]
    [InlineData("Measure")]
    [InlineData("On")]
    public async Task should_fold_the_declared_month_for_the_handler(string mode)
    {
        await Append("2026-10", 500);
        await Append("2026-11", 8000);
        object command = mode switch
        {
            "Off" => new Off(_account, "2026-10", 100),
            "Measure" => new Measure(_account, "2026-10", 100),
            _ => new Withdraw(_account, "2026-10", 100)
        };
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(command.GetType());
        Begin(command);
        var month = (MonthlyWithdrawals)Resolve()!;
        new Inspect().Handle(month).Amount.ShouldEqual(500);
        month.Id.ShouldEqual(_account);
        _unit.HasEnrolledDecisionReads.ShouldEqual(mode == "On");
        if (mode == "On")
        {
            var token = _services.GetRequiredService<DecisionRead<MonthlyWithdrawals>>();
            token.Instance.ShouldBeSame(month);
            token.EventStreamId.ShouldEqual((Cratis.Chronicle.Events.EventStreamId)"2026-10");
            token.IsProtected.ShouldBeTrue();
        }
        (await Complete()).IsSuccess.ShouldBeTrue();
    }

    public record Inspect
    {
        public MoneyWithdrawn Handle(MonthlyWithdrawals month) => new(month.Total);
    }
}
