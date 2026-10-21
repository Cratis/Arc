// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;

public class when_a_fresh_fold_is_incomplete : when_reading_a_stream.given.a_stream_reader
{
    [Fact]
    public async Task should_report_an_incomplete_on_read_as_a_retryable_validation_failure()
    {
        CommandTransaction.Current = _unit;
        Begin(typeof(GuardedCommand));
        using var frame = _unit.EnforceReadProtection();
        _inner.GetFor<Model>(_unit, (ReadModelKey)"owner", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DecisionRead<Model>>(new DecisionReadRefused(DecisionReadRefusalReason.FoldIncomplete, typeof(Model))));

        var exception = await Catch.Exception(async () => await _reader.Get<Model>((ReadModelKey)"owner"));

        exception.ShouldBeOfExactType<ReadIncomplete>();
        var result = ((IValidationFailure)exception).ValidationResult;
        result.Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
        result.ReasonDetail.ShouldEqual("ReadIncomplete");
        result.Message.ShouldEqual("Model 'owner' could not be read completely. Nothing was written; submit it again.");
        EndpointRouteHelper.GetStatusCode(false, true, false).ShouldEqual(System.Net.HttpStatusCode.BadRequest);
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }

    [Fact]
    public async Task should_keep_the_existing_protected_decision_failure_contract()
    {
        CommandTransaction.Current = _unit;
        Begin(typeof(ProtectedCommand));
        _inner.GetDetached<Model>((ReadModelKey)"owner", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DecisionRead<Model>>(new DecisionReadRefused(DecisionReadRefusalReason.FoldIncomplete, typeof(Model))));

        var exception = await Catch.Exception(async () => await _reader.Get<Model>((ReadModelKey)"owner"));

        exception.ShouldBeOfExactType<DecisionReadCouldNotBeAcquired>();
        ((DecisionReadRefused)exception!.InnerException!).Reason.ShouldEqual(DecisionReadRefusalReason.FoldIncomplete);
    }

    [ReadProtection(ReadProtectionMode.On)]
    public class GuardedCommand;
}
