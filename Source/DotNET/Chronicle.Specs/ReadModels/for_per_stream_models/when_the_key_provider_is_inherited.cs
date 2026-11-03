// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models;

public class when_the_key_provider_is_inherited : given.a_monthly_account
{
    IDisposable _policy;
    Exception _exception;

    void Establish()
    {
        var command = new Withdraw(_account, "2026-10", 100);
        _policy = DecisionPolicyForSpecs.Begin(command.GetType());
        Begin(command);
    }

    void Because() => _exception = Catch.Exception(() => Resolve(typeof(Dependencies).GetMethod(nameof(Dependencies.Handle))!.GetParameters()[0]));

    void Destroy() => _policy.Dispose();

    [Fact] void should_preserve_the_per_stream_prohibition() => _exception.ShouldBeOfExactType<InvalidLookUpBy>();
    [Fact] void should_report_the_safe_prohibition() => _exception.Message.ShouldContain("cannot redirect a per-stream read model; the stream belongs to the command's own event source");

    public class BaseDependencies
    {
        public virtual void Handle([LookUpBy<CallerSubject>] MonthlyWithdrawals? month) { }
    }

    public class Dependencies : BaseDependencies
    {
        public override void Handle(MonthlyWithdrawals? month) { }
    }
}
