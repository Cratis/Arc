// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;

public class when_a_validator_and_handle_read_the_same_model : Specification
{
    readonly ReadModelKey _key = (ReadModelKey)"source";
    UnitOfWork _unit;
    when_reading_a_stream.given.DecisionReaderForSpecs _inner;
    CommandDecisionReads _reader;

    void Establish()
    {
        var (store, _, unit) = DecisionFixtures.Transaction();
        _unit = unit;
        _inner = Substitute.For<when_reading_a_stream.given.DecisionReaderForSpecs>();
        var read = DecisionFixtures.Protected<Model>("source", new Model());
        _inner.GetFor<Model>(_unit, _key, Arg.Any<CancellationToken>()).Returns(read);
        _inner.GetDetached<Model>(_key, Arg.Any<CancellationToken>()).Returns(read);
        _reader = new CommandDecisionReads(_inner, store, Substitute.For<IReadModels>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_fold_once_per_execution(bool readProtectionOn)
    {
        var commandType = readProtectionOn ? typeof(ProtectedCommand) : typeof(OffCommand);
        CommandTransaction.Current = _unit;
        using var policy = DecisionPolicyForSpecs.Begin(commandType);
        CommandDecisionReads.Begin(commandType);
        try
        {
            var validator = await _reader.Get<Model>(_key);
            var handle = await _reader.Get<Model>(_key);
            handle.ShouldBeSame(validator);
            if (readProtectionOn)
            {
                await _inner.Received(1).GetFor<Model>(_unit, _key, Arg.Any<CancellationToken>());
                await _inner.DidNotReceive().GetDetached<Model>(_key, Arg.Any<CancellationToken>());
            }
            else
            {
                await _inner.Received(1).GetDetached<Model>(_key, Arg.Any<CancellationToken>());
                await _inner.DidNotReceive().GetFor<Model>(_unit, _key, Arg.Any<CancellationToken>());
            }
        }
        finally
        {
            CommandDecisionReads.End();
            CommandTransaction.Current = null;
        }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public class ProtectedCommand;

    [ProtectedDecision, ReadProtection(ReadProtectionMode.Off)]
    public class OffCommand;

    public class Model;
}
