// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_concept_parameters : given.a_model_bound_query_performer
{
    public record UserId(string Value) : ConceptAs<string>(Value)
    {
        public static readonly UserId NotSet = new(string.Empty);
    }

    public record ProductId(int Value) : ConceptAs<int>(Value)
    {
        public static readonly ProductId NotSet = new(0);
    }

    public record TestReadModel
    {
        public static UserId ReceivedUserId { get; set; } = UserId.NotSet;
        public static ProductId ReceivedProductId { get; set; } = ProductId.NotSet;
        public static string ReceivedName { get; set; } = string.Empty;

        public static TestReadModel Query(UserId userId, ProductId productId, string name)
        {
            ReceivedUserId = userId;
            ReceivedProductId = productId;
            ReceivedName = name;
            return new TestReadModel();
        }
    }

    void Establish()
    {
        var parameters = new QueryArguments
        {
            ["userId"] = "user123",
            ["productId"] = "456",
            ["name"] = "Test Product"
        };

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: parameters);
    }

    async Task Because() => await PerformQuery();

    [Fact] void should_convert_string_to_userId_concept() => TestReadModel.ReceivedUserId.Value.ShouldEqual("user123");
    [Fact] void should_convert_string_to_productId_concept() => TestReadModel.ReceivedProductId.Value.ShouldEqual(456);
    [Fact] void should_pass_regular_string_parameter() => TestReadModel.ReceivedName.ShouldEqual("Test Product");
}