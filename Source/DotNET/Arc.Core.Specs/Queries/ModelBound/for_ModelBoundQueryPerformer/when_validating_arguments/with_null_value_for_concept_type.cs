// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_validating_arguments;

/// <summary>
/// Concept types are value-like wrappers and are never implicitly nullable.
/// A missing or empty concept argument must produce <see cref="MissingArgumentForQuery"/>,
/// even though the underlying CLR type is a reference type.
/// </summary>
public class with_null_value_for_concept_type : given.a_model_bound_query_performer
{
    public record ItemId(Guid Value) : ConceptAs<Guid>(Value);

    public record TestReadModel
    {
        public static TestReadModel Query(ItemId id) => new();
    }

    MissingArgumentForQuery _exception;

    void Establish() => EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: new QueryArguments());

    async Task Because() => _exception = await Catch.Exception(PerformQuery) as MissingArgumentForQuery;

    [Fact] void should_throw_missing_argument_exception() => _exception.ShouldNotBeNull();
    [Fact] void should_report_the_parameter_name() => _exception.Message.ShouldContain("id");
}
