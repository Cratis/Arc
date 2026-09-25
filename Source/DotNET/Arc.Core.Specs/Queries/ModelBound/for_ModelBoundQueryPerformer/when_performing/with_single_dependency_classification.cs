// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_single_dependency_classification : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static TestReadModel Query(object dependency, string name) => new();
    }

    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(object)).Returns(true);
        _serviceProviderIsService.IsService(typeof(string)).Returns(false);
        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), [new object()], new QueryArguments { ["name"] = "Jane" });
    }

    async Task Because()
    {
        _ = _performer.Dependencies.ToArray();
        _ = _performer.Dependencies.ToArray();
        await PerformQuery();
        _ = _performer.Dependencies.ToArray();
    }

    [Fact] void should_classify_dependency_only_once() => _serviceProviderIsService.Received(1).IsService(typeof(object));
    [Fact] void should_classify_query_parameter_only_once() => _serviceProviderIsService.Received(1).IsService(typeof(string));
}
