// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_enumerable_query_parameter : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static IEnumerable<int> ReceivedIds { get; set; } = [];

        public static TestReadModel Query(IEnumerable<int> ids)
        {
            ReceivedIds = ids;
            return new TestReadModel();
        }
    }

    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(IEnumerable<int>)).Returns(true);

        // A repeated query string key (?ids=1&ids=2&ids=3) collapses into a single comma-separated value by the
        // time it reaches IHttpRequestContext.Query - this is the exact shape ModelBoundQueryPerformer must bind.
        var parameters = new QueryArguments { ["ids"] = "1,2,3" };
        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: parameters);
    }

    async Task Because() => await PerformQuery();

    [Fact] void should_bind_three_ids() => TestReadModel.ReceivedIds.Count().ShouldEqual(3);
    [Fact] void should_bind_first_id() => TestReadModel.ReceivedIds.ElementAt(0).ShouldEqual(1);
    [Fact] void should_bind_second_id() => TestReadModel.ReceivedIds.ElementAt(1).ShouldEqual(2);
    [Fact] void should_bind_third_id() => TestReadModel.ReceivedIds.ElementAt(2).ShouldEqual(3);
}
