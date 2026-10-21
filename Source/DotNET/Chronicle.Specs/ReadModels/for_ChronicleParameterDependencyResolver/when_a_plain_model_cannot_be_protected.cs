// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_plain_model_cannot_be_protected : given.protected_plain_models
{
    Exception? _failure;

    void Establish() => _inner.Describe<Balance>().Returns(new DecisionReadDescription(new(false, DecisionReadRefusalReason.ReducerDependencies), [], false));

    void Because()
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
        CommandTransaction.Current = _unit;
        using var frame = _unit.EnforceReadProtection();
        CommandDecisionReads.Begin(typeof(Transfer));
        try
        {
            _failure = Catch.Exception(() => Resolve(HandleParameters[0]));
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    [Fact] void should_refuse_before_reading() => _tokens.ShouldBeEmpty();
    [Fact] void should_identify_the_parameter() => _failure.ShouldBeOfExactType<ReadCannotBeProtected>();
    [Fact] void should_provide_the_prescribed_fix() => _failure!.Message.ShouldEqual("Transfer reads 'from' (Balance) through a reducer with constructor dependencies, so read protection cannot guard it. If it is only context, mark it [Unprotected(\"why\")]. Otherwise remove constructor dependencies from the reducer.");
    [Fact] void should_not_fall_back_to_a_stored_read() => _stored.DidNotReceiveWithAnyArgs().GetInstanceById(default!, default!);
}
