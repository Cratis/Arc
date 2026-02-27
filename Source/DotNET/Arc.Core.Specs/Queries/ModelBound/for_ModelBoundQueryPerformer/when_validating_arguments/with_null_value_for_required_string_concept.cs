// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_validating_arguments;

/// <summary>
/// ConceptAs&lt;string&gt; parameters are required by the same rule as other concept types:
/// they are value-like wrappers and never implicitly nullable, regardless of their underlying type.
/// </summary>
public class with_null_value_for_required_string_concept : given.a_model_bound_query_performer
{
    public record EventId(string Value) : ConceptAs<string>(Value);

    public record TestReadModel
    {
        public static TestReadModel Query(EventId id) => new();
    }

    MissingArgumentForQuery _exception;

    void Establish() => EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: new QueryArguments());

    async Task Because() => _exception = await Catch.Exception(PerformQuery) as MissingArgumentForQuery;

    [Fact] void should_throw_missing_argument_exception() => _exception.ShouldNotBeNull();
    [Fact] void should_report_the_parameter_name() => _exception.Message.ShouldContain("id");
}
