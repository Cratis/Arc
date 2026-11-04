// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.when_binding_asynchronously;

public class and_acquisition_refuses_the_read : given.protected_plain_models
{
    [Theory]
    [InlineData(DecisionReadRefusalReason.InvalidKey, typeof(ProtectedReadModelDoesNotExist))]
    [InlineData(DecisionReadRefusalReason.FoldIncomplete, typeof(ReadIncomplete))]
    [InlineData(DecisionReadRefusalReason.Join, typeof(ReadCannotBeProtected))]
    public async Task should_preserve_the_typed_refusal(DecisionReadRefusalReason reason, Type failureType)
    {
        _inner.GetFor<Balance>(_unit, (ReadModelKey)"from", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DecisionRead<Balance>>(new DecisionReadRefused(reason, typeof(Balance))));
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
        CommandTransaction.Current = _unit;
        using var frame = _unit.EnforceReadProtection();
        CommandDecisionReads.Begin(typeof(Transfer));
        try
        {
            var failure = await Catch.Exception(async () => await ParameterDependencyResolver.ResolveAsync(_services, HandleParameters[0], _ => new InvalidOperationException()));
            failure.GetType().ShouldEqual(failureType);
            _tokens.ShouldBeEmpty();
            _stored.ReceivedCalls().ShouldBeEmpty();
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }
}
