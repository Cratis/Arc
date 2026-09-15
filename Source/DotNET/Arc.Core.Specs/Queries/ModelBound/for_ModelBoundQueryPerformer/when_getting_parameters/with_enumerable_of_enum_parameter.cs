// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_getting_parameters;

public class with_enumerable_of_enum_parameter : given.a_model_bound_query_performer
{
    public enum Status
    {
        Active = 0,
        Inactive = 1
    }

    public record TestReadModel
    {
        public static TestReadModel Query(IEnumerable<Status> statuses) => new();
    }

    QueryParameters _result;

    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(IEnumerable<Status>)).Returns(true);

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query));
        _result = _performer.Parameters;
    }

    [Fact] void should_have_one_parameter() => _result.Count.ShouldEqual(1);
    [Fact] void should_have_statuses_parameter() => _result.Any(p => p.Name == "statuses" && p.Type == typeof(IEnumerable<Status>)).ShouldBeTrue();
    [Fact] void should_not_be_classified_as_a_dependency() => _performer.Dependencies.Any(t => t == typeof(IEnumerable<Status>)).ShouldBeFalse();
}
