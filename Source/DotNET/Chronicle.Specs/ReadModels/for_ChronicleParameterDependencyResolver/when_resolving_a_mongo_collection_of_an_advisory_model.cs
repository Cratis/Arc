// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_resolving_a_mongo_collection_of_an_advisory_model : given.a_mongo_collection_parameter
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_resolve_the_collection_and_report_the_model_advisory_reason(bool asynchronously)
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(ReadCommand));

        var result = await Resolve(nameof(ReadCommand.FromType), asynchronously);

        result.ShouldBeSame(_advisoryCollection);
        AdvisoryTag("category").ShouldEqual("advisory_read");
        AdvisoryTag("model").ShouldEqual(typeof(AdvisoryBalance).FullName);
        AdvisoryTag("reason").ShouldEqual("display only");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_prefer_the_parameter_reason_to_the_model_reason(bool asynchronously)
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(ReadCommand));

        var result = await Resolve(nameof(ReadCommand.OverrideReason), asynchronously);

        result.ShouldBeSame(_advisoryCollection);
        AdvisoryTag("reason").ShouldEqual("parameter reason");
    }
}
