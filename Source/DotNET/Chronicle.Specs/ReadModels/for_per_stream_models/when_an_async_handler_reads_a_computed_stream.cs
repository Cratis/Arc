// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models;

public class when_an_async_handler_reads_a_computed_stream : given.a_monthly_account
{
    [Theory]
    [InlineData("2026-10", false)]
    [InlineData("2026-11", true)]
    public async Task should_enroll_before_returning_and_guard_only_the_read_month(string competitorMonth, bool succeeds)
    {
        await Append("2026-10", 500);
        await Append("2026-11", 8000);
        var command = new InspectMonth(_account, "2026-10-15");
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(InspectMonth));
        Begin(command);
        var reads = _services.GetRequiredService<IDecisionReads>();
        var context = new CommandContext(Cratis.Execution.CorrelationId.New(), typeof(InspectMonth), command, [reads], [], null, null, _services);
        var handler = new ModelBoundCommandHandler(typeof(InspectMonth), typeof(InspectMonth).GetMethod(nameof(InspectMonth.Handle))!);
        ((MoneyWithdrawn)(await handler.Handle(context))!).Amount.ShouldEqual(500);
        _unit.HasEnrolledDecisionReads.ShouldBeTrue();
        var token = await reads.Get<AccountWithdrawals>(_account, "Transactions", command.Month, "BankAccount", TestContext.Current.CancellationToken);
        token.IsProtected.ShouldBeTrue();
        token.EventStreamId.ShouldEqual((EventStreamId)command.Month);
        await Compete(competitorMonth);
        var result = await Complete();
        result.IsSuccess.ShouldEqual(succeeds);
        if (!succeeds)
        {
            result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
            result.ValidationResults.Single().ReasonDetail.ShouldEqual("ReadChanged");
        }
    }

    [Command, ReadProtection(ReadProtectionMode.On)]
    [EventSourceType("BankAccount")]
    [EventStreamType("Transactions")]
    [EventStreamId("{Month}")]
    public record InspectMonth(EventSourceId Id, string Date)
    {
        public string Month => Date[..7];

#pragma warning disable ARCCHR0040 // Computed month; https://github.com/Cratis/Arc/issues/3620 tracks acquisition gaps.
        public async Task<MoneyWithdrawn> Handle(IDecisionReads reads)
        {
            var state = await reads.Get<AccountWithdrawals>(Id, "Transactions", Month, "BankAccount");
            return new(state.Instance?.Total ?? 0);
        }
#pragma warning restore ARCCHR0040
    }
}
