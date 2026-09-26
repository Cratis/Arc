// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_nullable_concept_parameter_the_container_can_resolve : given.a_model_bound_query_performer
{
    public record Rate(decimal Value) : ConceptAs<decimal>(Value)
    {
        public static readonly Rate NotSet = new(0m);
    }

    public record TestReadModel
    {
        public static Rate? ReceivedRate { get; set; }

        public static TestReadModel Query(Rate? rate)
        {
            ReceivedRate = rate;
            return new TestReadModel();
        }
    }

    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(Rate)).Returns(true);

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: new QueryArguments { ["rate"] = "12.5" });
    }

    async Task Because() => await PerformQuery();

    [Fact] void should_bind_the_concept_from_the_request() => TestReadModel.ReceivedRate!.Value.ShouldEqual(12.5m);
    [Fact] void should_expose_the_concept_as_an_optional_query_parameter() => _performer.Parameters.Any(p => p.Name == "rate" && p.Type == typeof(Rate) && !p.IsRequired).ShouldBeTrue();
    [Fact] void should_not_classify_the_concept_as_a_dependency() => _performer.Dependencies.ShouldBeEmpty();
}
