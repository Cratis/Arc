// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

/// <summary>
/// ConceptAs&lt;string&gt; (e.g. EventSourceId, Name) can legitimately hold an empty string,
/// so an empty query-string value must be passed through as-is rather than treated as missing.
/// </summary>
public class with_empty_string_value_for_string_concept_parameter : given.a_model_bound_query_performer
{
    public record EventId(string Value) : ConceptAs<string>(Value);

    public record TestReadModel
    {
        public static EventId ReceivedId { get; set; } = new("not-set");

        public static TestReadModel Query(EventId id)
        {
            ReceivedId = id;
            return new();
        }
    }

    void Establish() => EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: new QueryArguments { ["id"] = string.Empty });

    async Task Because() => await PerformQuery();

    [Fact] void should_not_throw() => _result.ShouldNotBeNull();
    [Fact] void should_receive_concept_with_empty_string_value() => TestReadModel.ReceivedId.Value.ShouldEqual(string.Empty);
}
