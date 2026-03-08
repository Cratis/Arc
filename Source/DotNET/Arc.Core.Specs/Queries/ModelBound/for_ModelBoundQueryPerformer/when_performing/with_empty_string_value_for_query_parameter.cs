// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

/// <summary>
/// An empty query-string value (e.g. ?id=) is treated as "not provided" for types
/// that cannot represent an empty string as a meaningful value (e.g. ConceptAs&lt;Guid&gt;, int).
/// Plain string and ConceptAs&lt;string&gt; are excluded because "" is a valid value for them.
/// </summary>
public class with_empty_string_value_for_query_parameter : given.a_model_bound_query_performer
{
    public record ItemId(Guid Value) : ConceptAs<Guid>(Value);

    public record TestReadModel
    {
        public static TestReadModel QueryByItemId(ItemId id) => new();
    }

    MissingArgumentForQuery _guidConceptException;

    async Task Because()
    {
        EstablishPerformer<TestReadModel>(nameof(TestReadModel.QueryByItemId), parameters: new QueryArguments { ["id"] = string.Empty });
        _guidConceptException = await Catch.Exception(PerformQuery) as MissingArgumentForQuery;
    }

    [Fact] void should_treat_empty_string_as_missing_for_non_string_concept() => _guidConceptException.ShouldNotBeNull();
}
