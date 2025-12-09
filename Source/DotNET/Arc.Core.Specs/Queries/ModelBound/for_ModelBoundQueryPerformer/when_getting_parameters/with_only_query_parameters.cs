// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_getting_parameters;

public class with_only_query_parameters : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static TestReadModel Query(string name, int age, bool isActive)
        {
            return new TestReadModel();
        }
    }

    QueryParameters _result;

    void Establish()
    {
        // Configure mock so that all types are NOT services (they are query parameters)
        _serviceProviderIsService.IsService(typeof(string)).Returns(false);
        _serviceProviderIsService.IsService(typeof(int)).Returns(false);
        _serviceProviderIsService.IsService(typeof(bool)).Returns(false);

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query));
        _result = _performer.Parameters;
    }

    [Fact] void should_have_three_parameters() => _result.Count.ShouldEqual(3);
    [Fact] void should_have_name_parameter() => _result.Any(p => p.Name == "name" && p.Type == typeof(string)).ShouldBeTrue();
    [Fact] void should_have_age_parameter() => _result.Any(p => p.Name == "age" && p.Type == typeof(int)).ShouldBeTrue();
    [Fact] void should_have_isActive_parameter() => _result.Any(p => p.Name == "isActive" && p.Type == typeof(bool)).ShouldBeTrue();
}