// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_getting_parameters;

public class with_mixed_dependencies_and_query_parameters : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static TestReadModel Query(IServiceProviderIsService dependency, string name, int age)
        {
            return new TestReadModel();
        }
    }

    QueryParameters _result;

    void Establish()
    {
        // Configure mock so that IServiceProviderIsService is a service (dependency)
        // while string and int are query parameters
        _serviceProviderIsService.IsService(typeof(IServiceProviderIsService)).Returns(true);
        _serviceProviderIsService.IsService(typeof(string)).Returns(false);
        _serviceProviderIsService.IsService(typeof(int)).Returns(false);

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query));
        _result = _performer.Parameters;
    }

    [Fact] void should_have_two_parameters() => _result.Count.ShouldEqual(2);
    [Fact] void should_have_name_parameter() => _result.Any(p => p.Name == "name" && p.Type == typeof(string)).ShouldBeTrue();
    [Fact] void should_have_age_parameter() => _result.Any(p => p.Name == "age" && p.Type == typeof(int)).ShouldBeTrue();
    [Fact] void should_not_include_dependency_parameter() => _result.Any(p => p.Type == typeof(IServiceProviderIsService)).ShouldBeFalse();
}