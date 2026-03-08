// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_validating_arguments;

/// <summary>
/// A concept parameter annotated as T? is optional regardless of the underlying type.
/// </summary>
public class with_null_value_for_nullable_int_concept : given.a_model_bound_query_performer
{
    public record PageNumber(int Value) : ConceptAs<int>(Value);

    public record TestReadModel
    {
        public static PageNumber? ReceivedPage { get; set; } = new(1);

        public static TestReadModel Query(PageNumber? page)
        {
            ReceivedPage = page;
            return new();
        }
    }

    void Establish() => EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: new QueryArguments());

    async Task Because() => await PerformQuery();

    [Fact] void should_accept_missing_argument() => _result.ShouldNotBeNull();
    [Fact] void should_receive_null_for_the_concept_parameter() => TestReadModel.ReceivedPage.ShouldBeNull();
}
