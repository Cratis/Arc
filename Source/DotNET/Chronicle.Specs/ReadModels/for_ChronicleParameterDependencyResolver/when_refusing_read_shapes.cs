// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_refusing_read_shapes : given.protected_plain_models
{
    [Theory]
    [InlineData(DecisionReadRefusalReason.ReducerDependencies, "remove constructor dependencies from the reducer")]
    [InlineData(DecisionReadRefusalReason.ReducerFilters, "remove the reducer's event filter")]
    [InlineData(DecisionReadRefusalReason.Join, "read the parts as separate models")]
    [InlineData(DecisionReadRefusalReason.Hierarchy, "read the parts as separate models")]
    [InlineData(DecisionReadRefusalReason.FromEventProperty, "append a fact to the entity it is keyed by, or use a constraint")]
    [InlineData(DecisionReadRefusalReason.NotEventSourceKeyed, "append a fact to the entity it is keyed by, or use a constraint")]
    [InlineData(DecisionReadRefusalReason.GenerationDeliveryNotSupported, "use Compatibility delivery for this projection")]
    [InlineData(DecisionReadRefusalReason.KeyConversion, "give Balance a string or Guid Id")]
    [InlineData(DecisionReadRefusalReason.NotEventLog, "read a model of the event log")]
    [InlineData(DecisionReadRefusalReason.StreamKeyRequiresStreamScope, "use IDecisionReads.Get with an explicit source")]
    public void should_name_the_parameter_and_the_fix(DecisionReadRefusalReason reason, string fix)
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
        CommandTransaction.Current = _unit;
        using var frame = _unit.EnforceReadProtection();
        CommandDecisionReads.Begin(typeof(Transfer));
        try
        {
            _inner.Describe<Balance>().Returns(new DecisionReadDescription(new(false, reason), [], false));
            var exception = Catch.Exception(() => Resolve(HandleParameters[0]));
            exception.ShouldBeOfExactType<ReadCannotBeProtected>();
            exception.Message.ShouldContain("Transfer reads 'from' (Balance)");
            exception.Message.ShouldContain(fix);
            exception.Message.ShouldContain("[Unprotected(\"why\")]");
            _tokens.ShouldBeEmpty();
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    [Fact]
    public void should_refuse_a_chronicle_mongo_collection_at_resolution()
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
        var parameter = typeof(Transfer).GetMethod(nameof(Transfer.FromDatabase))!.GetParameters()[0];
        var failure = Catch.Exception(() => Resolve(parameter));
        failure.ShouldBeOfExactType<ReadCannotBeProtected>();
        failure!.Message.ShouldContain("inject Balance");
        _tokens.ShouldBeEmpty();
    }

    [Fact]
    public void should_refuse_a_pinned_model_even_when_its_other_shape_is_admitted()
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
        _inner.Describe<Balance>().Returns(new DecisionReadDescription(new(true, null), [], true));
        Catch.Exception(() => Resolve(HandleParameters[0])).ShouldBeOfExactType<ReadCannotBeProtected>();
        _tokens.ShouldBeEmpty();
    }
}
