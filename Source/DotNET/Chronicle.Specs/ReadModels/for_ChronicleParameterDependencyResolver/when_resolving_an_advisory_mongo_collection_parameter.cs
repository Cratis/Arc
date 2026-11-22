// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_resolving_an_advisory_mongo_collection_parameter : given.a_mongo_collection_parameter
{
    [Theory]
    [InlineData(false, nameof(ReadCommand.Handle))]
    [InlineData(true, nameof(ReadCommand.Handle))]
    [InlineData(false, nameof(Validator))]
    [InlineData(true, nameof(Validator))]
    public async Task should_resolve_the_collection_and_report_the_declared_advisory_read(bool asynchronously, string method)
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(ReadCommand));

        var result = await Resolve(method, asynchronously);

        result.ShouldBeSame(_collection);
        AdvisoryTag("command").ShouldEqual(typeof(ReadCommand).FullName);
        AdvisoryTag("category").ShouldEqual("advisory_read");
        AdvisoryTag("model").ShouldEqual(typeof(Balance).FullName);
        AdvisoryTag("reason").ShouldEqual("context only");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_report_the_default_advisory_reason_when_none_is_supplied(bool asynchronously)
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(ReadCommand));

        var result = await Resolve(nameof(ReadCommand.WithoutReason), asynchronously);

        result.ShouldBeSame(_collection);
        AdvisoryTag("reason").ShouldEqual("Declared advisory");
    }
}
