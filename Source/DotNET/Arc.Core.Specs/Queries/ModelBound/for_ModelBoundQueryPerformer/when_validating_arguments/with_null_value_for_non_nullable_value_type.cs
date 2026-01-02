// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_validating_arguments;

public class with_null_value_for_non_nullable_value_type : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static TestReadModel Query(int requiredInt)
        {
            return new TestReadModel();
        }
    }

    MissingArgumentForQuery _exception;

    void Establish()
    {
        var parameters = new QueryArguments();
        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: parameters);
    }

    async Task Because() => _exception = await Catch.Exception(PerformQuery) as MissingArgumentForQuery;

    [Fact] void should_throw_missing_argument_exception() => _exception.ShouldNotBeNull();
    [Fact] void should_have_parameter_name_in_exception() => _exception.Message.ShouldContain("requiredInt");
}
