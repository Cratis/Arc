// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_getting_parameters;

public class with_only_dependencies : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static TestReadModel Query(IServiceProviderIsService firstDependency, IServiceProvider secondDependency)
        {
            return new TestReadModel();
        }
    }

    QueryParameters _result;

    void Establish()
    {
        // Configure mock so that both types are services (dependencies)
        _serviceProviderIsService.IsService(typeof(IServiceProviderIsService)).Returns(true);
        _serviceProviderIsService.IsService(typeof(IServiceProvider)).Returns(true);

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query));
        _result = _performer.Parameters;
    }

    [Fact] void should_have_no_parameters() => _result.Count.ShouldEqual(0);
    [Fact] void should_be_empty() => _result.ShouldBeEmpty();
}