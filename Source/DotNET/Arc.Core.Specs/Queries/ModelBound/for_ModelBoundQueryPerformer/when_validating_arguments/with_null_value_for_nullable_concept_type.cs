// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_validating_arguments;

/// <summary>
/// A concept parameter annotated as T? should be accepted as optional.
/// The C# compiler emits a [Nullable] attribute that <see cref="System.Reflection.NullabilityInfoContext"/>
/// reads at runtime, so EventSourceId? is distinguishable from EventSourceId even though
/// both share the same CLR type.
/// </summary>
public class with_null_value_for_nullable_concept_type : given.a_model_bound_query_performer
{
    public record ItemId(Guid Value) : ConceptAs<Guid>(Value);

    public record TestReadModel
    {
        public static ItemId? ReceivedId { get; set; }

        public static TestReadModel Query(ItemId? id)
        {
            ReceivedId = id;
            return new();
        }
    }

    void Establish() => EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: new QueryArguments());

    async Task Because() => await PerformQuery();

    [Fact] void should_accept_missing_argument() => _result.ShouldNotBeNull();
    [Fact] void should_receive_null_for_the_concept_parameter() => TestReadModel.ReceivedId.ShouldBeNull();
}
