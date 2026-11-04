// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models;

public class when_an_awaited_protected_stream_model_is_missing : given.a_monthly_account
{
    Exception _failure;

    async Task Because()
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Withdraw));
        Begin(new Withdraw(_account, "2026-10", 100));
        var parameter = typeof(Withdraw).GetMethod(nameof(Withdraw.Required))!.GetParameters()[0];
        _failure = await Catch.Exception(async () => await ParameterDependencyResolver.ResolveAsync(_services, parameter, _ => new InvalidOperationException()));
    }

    [Fact] void should_preserve_the_missing_model_type() => _failure.ShouldBeOfExactType<ProtectedReadModelDoesNotExist>();
    [Fact] void should_preserve_the_rule_failure() => ((IValidationFailure)_failure).ValidationResult.Reason.ShouldEqual(ValidationResultReason.Rule);
    [Fact] void should_preserve_the_missing_model_key() => ((IValidationFailure)_failure).ValidationResult.Message.ShouldEqual("MonthlyWithdrawals 'account' does not exist.");
    [Fact] void should_keep_the_absence_guard_enrolled() => _unit.HasEnrolledDecisionReads.ShouldBeTrue();
}
