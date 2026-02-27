// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_validating_arguments;

/// <summary>
/// Concept types backed by int (or any non-string value type) are required by the same rule:
/// they are value-like wrappers and never implicitly nullable.
/// </summary>
public class with_null_value_for_required_int_concept : given.a_model_bound_query_performer
{
    public record PageNumber(int Value) : ConceptAs<int>(Value);

    public record TestReadModel
    {
        public static TestReadModel Query(PageNumber page) => new();
    }

    MissingArgumentForQuery _exception;

    void Establish() => EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: new QueryArguments());

    async Task Because() => _exception = await Catch.Exception(PerformQuery) as MissingArgumentForQuery;

    [Fact] void should_throw_missing_argument_exception() => _exception.ShouldNotBeNull();
    [Fact] void should_report_the_parameter_name() => _exception.Message.ShouldContain("page");
}
