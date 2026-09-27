// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;

public class when_resolving_an_unprotected_read_twice : Specification
{
    readonly ReadModelKey _key = (ReadModelKey)"source";
    DecisionRead<SomeModel> _first;
    DecisionRead<SomeModel> _second;
    IDecisionReads _inner;
    IReadModels _readModels;

    void Establish()
    {
        _inner = Substitute.For<IDecisionReads>();
        _readModels = Substitute.For<IReadModels>();
        var model = new SomeModel();
        _readModels.GetInstanceById<SomeModel>(_key).Returns(model);
        _readModels.Release(model).Returns(model);
    }

    async Task Because()
    {
        var store = Substitute.For<IEventStore>();
        store.Name.Returns((EventStoreName)"store");
        store.Namespace.Returns((EventStoreNamespaceName)"namespace");
        var reader = new CommandDecisionReads(_inner, store, _readModels);
        using var policy = DecisionPolicyForSpecs.Begin(typeof(UnprotectedCommand));
        CommandDecisionReads.Begin(typeof(UnprotectedCommand));
        try
        {
            _first = await reader.Get<SomeModel>(_key);
            _second = await reader.Get<SomeModel>(_key);
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    [Fact] void should_share_one_read_within_the_invocation() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_be_unguarded() => _first.IsProtected.ShouldBeFalse();
    [Fact] void should_release_legacy_read_model() => _readModels.Received(1).Release(Arg.Any<SomeModel>());
    [Fact] void should_not_fold_a_protected_model() => _inner.DidNotReceiveWithAnyArgs().GetDetached<SomeModel>(_key);

    [Unprotected]
    public class UnprotectedCommand;
    public class SomeModel;
}
