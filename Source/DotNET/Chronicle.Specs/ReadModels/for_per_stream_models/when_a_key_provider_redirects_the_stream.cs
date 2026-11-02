// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models;

public class when_a_key_provider_redirects_the_stream : given.a_monthly_account
{
    [Theory]
    [InlineData("Off")]
    [InlineData("Measure")]
    [InlineData("On")]
    public void should_refuse_the_provider_without_redirecting_the_stream(string mode)
    {
        var command = mode switch
        {
            "Off" => new Off(_account, "2026-10", 100),
            "Measure" => new Measure(_account, "2026-10", 100),
            _ => new Withdraw(_account, "2026-10", 100)
        };
        using var policy = DecisionPolicyForSpecs.Begin(command.GetType());
        Begin(command);

        var failure = Catch.Exception(() => Resolve(typeof(Dependencies).GetMethod(nameof(Dependencies.Handle))!.GetParameters()[0]));

        failure.ShouldBeOfExactType<InvalidLookUpBy>();
        failure.Message.ShouldContain("cannot redirect a per-stream read model; the stream belongs to the command's own event source");
        failure.Message.ShouldContain(nameof(CallerSubject));
    }

    public class Dependencies
    {
        public void Handle([LookUpBy<CallerSubject>] MonthlyWithdrawals? month) { }
    }
}
