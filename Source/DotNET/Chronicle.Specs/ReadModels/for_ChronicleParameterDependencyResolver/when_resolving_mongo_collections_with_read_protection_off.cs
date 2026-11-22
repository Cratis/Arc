// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_resolving_mongo_collections_with_read_protection_off : given.a_mongo_collection_parameter
{
    [Theory]
    [InlineData(false, nameof(ReadCommand.Handle))]
    [InlineData(true, nameof(ReadCommand.Handle))]
    [InlineData(false, nameof(ReadCommand.WithoutAnnotation))]
    [InlineData(true, nameof(ReadCommand.WithoutAnnotation))]
    [InlineData(false, nameof(Validator))]
    [InlineData(true, nameof(Validator))]
    public async Task should_resolve_normally_without_advisory_telemetry(bool asynchronously, string method)
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(OffCommand));

        var result = await Resolve(method, asynchronously);

        result.ShouldBeSame(_collection);
        _activity.Events.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_resolve_an_advisory_model_without_reporting_an_advisory_read(bool asynchronously)
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(OffCommand));

        var result = await Resolve(nameof(ReadCommand.FromType), asynchronously);

        result.ShouldBeSame(_advisoryCollection);
        _activity.Events.ShouldBeEmpty();
    }
}
