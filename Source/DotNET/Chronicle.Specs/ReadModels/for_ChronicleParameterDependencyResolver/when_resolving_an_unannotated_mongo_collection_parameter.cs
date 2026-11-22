// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_resolving_an_unannotated_mongo_collection_parameter : given.a_mongo_collection_parameter
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_the_refusal_and_its_message(bool asynchronously)
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(ReadCommand));

        var failure = await Catch.Exception(async () => await Resolve(nameof(ReadCommand.WithoutAnnotation), asynchronously));

        failure.ShouldBeOfExactType<ReadCannotBeProtected>();
        failure.Message.ShouldEqual(new ReadCannotBeProtected(typeof(ReadCommand), "collection", typeof(Balance), "through IMongoCollection", "inject Balance").Message);
        _activity.Events.ShouldBeEmpty();
    }
}
