// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_getting_parameters;

public class with_a_cancellation_token_parameter : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static TestReadModel Query(string name, CancellationToken cancellationToken)
        {
            return new TestReadModel();
        }
    }

    QueryParameters _result;

    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(string)).Returns(false);

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query));
        _result = _performer.Parameters;
    }

    [Fact] void should_only_expose_the_callers_argument() => _result.Count.ShouldEqual(1);
    [Fact] void should_expose_the_name_argument() => _result.First().Name.ShouldEqual("name");
    [Fact] void should_not_be_a_dependency() => _performer.Dependencies.ShouldBeEmpty();
}
