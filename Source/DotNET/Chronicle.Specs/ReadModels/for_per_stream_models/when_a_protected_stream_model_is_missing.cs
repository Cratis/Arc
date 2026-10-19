// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models;

public class when_a_protected_stream_model_is_missing : given.a_monthly_account
{
    [Fact]
    public void should_reject_a_missing_required_stream_model()
    {
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Withdraw));
        Begin(new Withdraw(_account, "2026-10", 100));

        var failure = Catch.Exception(() => Resolve(nameof(Withdraw.Required)));

        failure.ShouldBeOfExactType<ProtectedReadModelDoesNotExist>();
        var result = ((IValidationFailure)failure).ValidationResult;
        result.Reason.ShouldEqual(ValidationResultReason.Rule);
        result.Message.ShouldEqual("MonthlyWithdrawals 'account' does not exist.");
    }

    [Fact]
    public void should_keep_nullable_stream_absence()
    {
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Withdraw));
        Begin(new Withdraw(_account, "2026-10", 100));

        Resolve().ShouldBeNull();

        _unit.HasEnrolledDecisionReads.ShouldBeTrue();
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("*")]
    [InlineData("a#b")]
    [InlineData(" padded ")]
    public void should_reject_an_invalid_source_before_enrollment(string source)
    {
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Withdraw));
        Begin(new Withdraw(_account, "2026-10", 100));
        _context.Values[WellKnownCommandContextKeys.EventSourceId] = (EventSourceId)source;

        var failure = Catch.Exception(() => Resolve());

        failure.ShouldBeOfExactType<ProtectedReadModelDoesNotExist>();
        ((IValidationFailure)failure).ValidationResult.Message.ShouldEqual($"MonthlyWithdrawals '{source}' does not exist.");
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }
}
