// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_invalid_concept_collection_element : given.a_model_bound_query_performer
{
    MissingArgumentForQuery _exception;

    public record Rate(decimal Value) : ConceptAs<decimal>(Value);

    public record TestReadModel
    {
        public static bool WasCalled { get; set; }

        public static TestReadModel Query(IEnumerable<Rate> rates)
        {
            WasCalled = true;
            return new TestReadModel();
        }
    }

    void Establish()
    {
        TestReadModel.WasCalled = false;
        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: new QueryArguments { ["rates"] = "1,bad" });
    }

    async Task Because() => _exception = await Catch.Exception(PerformQuery) as MissingArgumentForQuery;

    [Fact] void should_reject_the_invalid_element() => _exception.ShouldNotBeNull();
    [Fact] void should_name_the_argument_in_the_validation_result() => _exception.ValidationResult.Members.ShouldContain("rates");
    [Fact] void should_have_a_validation_failure() => (_exception is IValidationFailure).ShouldBeTrue();
    [Fact] void should_not_invoke_the_query() => TestReadModel.WasCalled.ShouldBeFalse();
}
