// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;

public class when_overriding_a_plain_read_model_instance : Specification
{
    readonly ReadModelKey _key = (ReadModelKey)"source";
    readonly Model _pin = new();
    when_reading_a_stream.given.DecisionReaderForSpecs _inner;
    UnitOfWork _unit;
    CommandDecisionReads _reader;
    DecisionRead<Model> _token;
    DecisionRead<Model> _read;
    Model? _instance;
    IDisposable _policy;

    void Establish()
    {
        var (store, _, unit) = DecisionFixtures.Transaction();
        _unit = unit;
        _inner = Substitute.For<when_reading_a_stream.given.DecisionReaderForSpecs>();
        _token = DecisionFixtures.Protected<Model>("source");
        _inner.GetFor<Model>(_unit, _key, Arg.Any<CancellationToken>()).Returns(_token);
        var models = new CommandScenarioReadModels(Substitute.For<IReadModels>());
        models.SeedInstance(typeof(Model), "source", _pin);
        _reader = new(_inner, store, models);
        CommandTransaction.Current = _unit;
        _policy = DecisionPolicyForSpecs.Begin(typeof(ProtectedCommand));
        CommandDecisionReads.Begin(typeof(ProtectedCommand));
    }

    async Task Because()
    {
        _instance = await _reader.GetInstance<Model>(_key, global::Xunit.TestContext.Current.CancellationToken);
        _read = await _reader.Get<Model>(_key, global::Xunit.TestContext.Current.CancellationToken);
    }

    [Fact] void should_supply_the_pin_to_the_plain_parameter() => _instance.ShouldBeSame(_pin);
    [Fact] void should_leave_the_real_token_unchanged() => _read.ShouldBeSame(_token);
    [Fact] void should_not_put_the_pin_in_the_token() => _read.Instance.ShouldBeNull();
    [Fact] async Task should_acquire_the_enrolled_read() => await _inner.Received(1).GetFor<Model>(_unit, _key, Arg.Any<CancellationToken>());
    [Fact] void should_keep_the_seam_internal() => typeof(IGivenReadModelInstances).IsVisible.ShouldBeFalse();
    [Fact] void should_not_export_a_substitutable_hook() => typeof(CommandDecisionReads).Assembly.ExportedTypes.Any(type => type.Name == nameof(IGivenReadModelInstances)).ShouldBeFalse();

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
