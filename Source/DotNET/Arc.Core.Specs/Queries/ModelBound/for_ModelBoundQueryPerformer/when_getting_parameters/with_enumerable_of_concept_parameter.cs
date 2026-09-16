// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_getting_parameters;

public class with_enumerable_of_concept_parameter : given.a_model_bound_query_performer
{
    public record ProductCode(string Value) : ConceptAs<string>(Value)
    {
        public static readonly ProductCode NotSet = new(string.Empty);
    }

    public record TestReadModel
    {
        public static TestReadModel Query(IEnumerable<ProductCode> codes) => new();
    }

    QueryParameters _result;

    void Establish()
    {
        // A concept is a reference type, so it is never excluded by the IsValueType short-circuit. The mock still
        // answers true here to prove the collection is excluded before the container is even asked.
        _serviceProviderIsService.IsService(typeof(IEnumerable<ProductCode>)).Returns(true);

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query));
        _result = _performer.Parameters;
    }

    [Fact] void should_have_one_parameter() => _result.Count.ShouldEqual(1);
    [Fact] void should_have_codes_parameter() => _result.Any(p => p.Name == "codes" && p.Type == typeof(IEnumerable<ProductCode>)).ShouldBeTrue();
    [Fact] void should_not_be_classified_as_a_dependency() => _performer.Dependencies.Any(t => t == typeof(IEnumerable<ProductCode>)).ShouldBeFalse();
}
