// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

/// <summary>
/// When a non-string concept parameter has a default value and the query string sends "" for it,
/// the empty value cannot be represented by the concept's underlying type, so the default is used.
/// </summary>
public class with_empty_string_for_non_string_concept_with_default_value : given.a_model_bound_query_performer
{
    public record PageSize(int Value) : ConceptAs<int>(Value);

    public record TestReadModel
    {
        public static PageSize? ReceivedPageSize { get; set; } = new(0);

        public static TestReadModel Query(PageSize? pageSize = null)
        {
            ReceivedPageSize = pageSize;
            return new();
        }
    }

    void Establish() => EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: new QueryArguments { ["pageSize"] = string.Empty });

    async Task Because() => await PerformQuery();

    [Fact] void should_not_throw() => _result.ShouldNotBeNull();
    [Fact] void should_fall_back_to_the_default_value() => TestReadModel.ReceivedPageSize.ShouldBeNull();
}
