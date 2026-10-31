// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;

public class when_read_models_do_not_give_instances : Specification
{
    readonly Model _state = new();
    readonly Model _released = new();
    readonly ReadModelKey _key = (ReadModelKey)"source";
    IReadModels _models;
    CommandDecisionReads _reader;
    Model? _instance;
    IDisposable _policy;

    void Establish()
    {
        var (store, _, unit) = DecisionFixtures.Transaction();
        var inner = Substitute.For<when_reading_a_stream.given.DecisionReaderForSpecs>();
        _models = Substitute.For<IReadModels>();
        inner.GetFor<Model>(unit, _key, Arg.Any<CancellationToken>()).Returns(DecisionFixtures.Protected("source", _state));
        _models.Release(_state).Returns(_released);
        _reader = new(inner, store, _models);
        CommandTransaction.Current = unit;
        _policy = DecisionPolicyForSpecs.Begin(typeof(ProtectedCommand));
        CommandDecisionReads.Begin(typeof(ProtectedCommand));
    }

    async Task Because() => _instance = await _reader.GetInstance<Model>(_key, global::Xunit.TestContext.Current.CancellationToken);

    [Fact] void should_return_the_released_instance() => _instance.ShouldBeSame(_released);
    [Fact] async Task should_release_the_real_state() => await _models.Received(1).Release(_state);

    void Destroy()
    {
        CommandDecisionReads.End();
        _policy.Dispose();
        CommandTransaction.Current = null;
    }

    public class Model;
    [ReadProtection(ReadProtectionMode.On)]
    public class ProtectedCommand;
}
