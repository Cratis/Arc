// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_getting_parameters;

public class with_parameter_resolved_by_query_dependency_resolver : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static TestReadModel Query(IServiceProviderIsService serviceDependency, object resolverDependency, string queryParam)
        {
            return new TestReadModel();
        }
    }

    QueryParameters _parameters;
    IEnumerable<Type> _dependencies;
    IQueryDependencyResolver _resolver;

    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(IServiceProviderIsService)).Returns(true);
        _serviceProviderIsService.IsService(typeof(object)).Returns(false);
        _serviceProviderIsService.IsService(typeof(string)).Returns(false);

        _resolver = Substitute.For<IQueryDependencyResolver>();
        _resolver.CanResolve(typeof(object)).Returns(true);
        _resolver.CanResolve(typeof(IServiceProviderIsService)).Returns(false);
        _resolver.CanResolve(typeof(string)).Returns(false);

        var method = typeof(TestReadModel).GetMethod(nameof(TestReadModel.Query))!;
        _performer = new ModelBoundQueryPerformer(typeof(TestReadModel), method, _serviceProviderIsService, [_resolver], _authorizationEvaluator);

        _parameters = _performer.Parameters;
        _dependencies = _performer.Dependencies;
    }

    [Fact] void should_classify_resolver_resolvable_as_dependency() => _dependencies.ShouldContain(typeof(object));
    [Fact] void should_classify_service_as_dependency() => _dependencies.ShouldContain(typeof(IServiceProviderIsService));
    [Fact] void should_have_two_dependencies() => _dependencies.Count().ShouldEqual(2);
    [Fact] void should_have_one_query_parameter() => _parameters.Count.ShouldEqual(1);
    [Fact] void should_have_query_param_as_string() => _parameters.Any(p => p.Name == "queryParam" && p.Type == typeof(string)).ShouldBeTrue();
}
