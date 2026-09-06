// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_getting_parameters;

public class with_enumerable_of_primitive_parameter : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static TestReadModel Query(IEnumerable<int> ids) => new();
    }

    QueryParameters _result;

    void Establish()
    {
        // .NET's built-in IServiceProviderIsService answers true for any IEnumerable<T> unconditionally - the
        // container can always satisfy it with an empty collection. Mirroring that here proves the classification
        // no longer defers to the container for this shape.
        _serviceProviderIsService.IsService(typeof(IEnumerable<int>)).Returns(true);

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query));
        _result = _performer.Parameters;
    }

    [Fact] void should_have_one_parameter() => _result.Count.ShouldEqual(1);
    [Fact] void should_have_ids_parameter() => _result.Any(p => p.Name == "ids" && p.Type == typeof(IEnumerable<int>)).ShouldBeTrue();
    [Fact] void should_not_be_classified_as_a_dependency() => _performer.Dependencies.Any(t => t == typeof(IEnumerable<int>)).ShouldBeFalse();
}
