// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;

public class when_a_key_is_refused : when_reading_a_stream.given.a_stream_reader
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("*")]
    [InlineData("a#b")]
    [InlineData(" padded")]
    [InlineData("padded ")]
    public async Task should_reject_a_direct_on_read_before_the_reader_is_consulted(string key)
    {
        Begin(typeof(OnCommand));
        _inner.ClearReceivedCalls();

        var failure = await Catch.Exception(async () => await _reader.Get<Model>((ReadModelKey)key));

        AssertMissing(failure, key);
        _inner.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task should_map_a_chronicle_invalid_key_refusal_as_a_backstop()
    {
        CommandTransaction.Current = _unit;
        Begin(typeof(OnCommand));
        _inner.GetFor<Model>(_unit, (ReadModelKey)"owner", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DecisionRead<Model>>(new DecisionReadRefused(DecisionReadRefusalReason.InvalidKey, typeof(Model))));

        var failure = await Catch.Exception(async () => await _reader.Get<Model>((ReadModelKey)"owner"));

        AssertMissing(failure, "owner");
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }

    [Fact]
    public async Task should_leave_other_refusals_unchanged()
    {
        CommandTransaction.Current = _unit;
        Begin(typeof(OnCommand));
        var refused = new DecisionReadRefused(DecisionReadRefusalReason.DefinitionMismatch, typeof(Model));
        _inner.GetFor<Model>(_unit, (ReadModelKey)"owner", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DecisionRead<Model>>(refused));

        var failure = await Catch.Exception(async () => await _reader.Get<Model>((ReadModelKey)"owner"));

        failure.ShouldBeSame(refused);
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }

    [Fact]
    public async Task should_keep_the_explicit_protected_decision_contract_without_on()
    {
        CommandTransaction.Current = _unit;
        Begin(typeof(ProtectedCommand));
        _inner.GetDetached<Model>((ReadModelKey)"owner", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DecisionRead<Model>>(new DecisionReadRefused(DecisionReadRefusalReason.InvalidKey, typeof(Model))));

        var failure = await Catch.Exception(async () => await _reader.Get<Model>((ReadModelKey)"owner"));

        failure.ShouldBeOfExactType<DecisionReadCouldNotBeAcquired>();
        ((DecisionReadRefused)failure.InnerException!).Reason.ShouldEqual(DecisionReadRefusalReason.InvalidKey);
    }

    static void AssertMissing(Exception failure, string key)
    {
        failure.ShouldBeOfExactType<ProtectedReadModelDoesNotExist>();
        var result = ((IValidationFailure)failure).ValidationResult;
        result.Reason.ShouldEqual(ValidationResultReason.Rule);
        result.Message.ShouldEqual($"Model '{key}' does not exist.");
    }

    [ReadProtection(ReadProtectionMode.On)]
    public class OnCommand;
}
