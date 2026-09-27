// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_hash_set_query_parameter : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static HashSet<int> ReceivedIds { get; set; } = [];

        public static TestReadModel Query(HashSet<int> ids)
        {
            ReceivedIds = ids;
            return new TestReadModel();
        }
    }

    void Establish()
    {
        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: new QueryArguments { ["ids"] = "1,2,3" });
    }

    async Task Because() => await PerformQuery();

    [Fact] void should_bind_a_set_of_ids() => TestReadModel.ReceivedIds.SetEquals([1, 2, 3]).ShouldBeTrue();
}
