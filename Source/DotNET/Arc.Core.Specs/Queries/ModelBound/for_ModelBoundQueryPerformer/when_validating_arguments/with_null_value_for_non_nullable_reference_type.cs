// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_validating_arguments;

public class with_null_value_for_non_nullable_reference_type : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static TestReadModel Query(string requiredString)
        {
            return new TestReadModel();
        }
    }

    void Establish()
    {
        var parameters = new QueryArguments();
        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: parameters);
    }

    async Task Because() => await PerformQuery();

    [Fact] void should_accept_null_value_for_reference_type() => _result.ShouldNotBeNull();
}
