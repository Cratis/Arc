// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_discovering_query_candidates : Specification
{
    QueryPerformerProvider _provider;
    ITypes _types;
    IQueryMetadataRegistry _registry;
    IServiceProviderIsService _serviceProviderIsService;
    IAuthorizationEvaluator _authorizationEvaluator;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _types.All.Returns([typeof(QueryCandidateReadModel)]);
        _registry = Substitute.For<IQueryMetadataRegistry>();
        _registry.All.Returns(new Dictionary<string, Type>());
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
    }

    void Because() => _provider = new QueryPerformerProvider(_types, _registry, _serviceProviderIsService, _authorizationEvaluator);

    [Fact] void should_discover_two_queries() => _provider.Performers.Count().ShouldEqual(2);
    [Fact] void should_discover_the_public_query() => _provider.Performers.Any(_ => _.FullyQualifiedName.Value == $"{typeof(QueryCandidateReadModel).FullName}.{nameof(QueryCandidateReadModel.ById)}").ShouldBeTrue();
    [Fact] void should_discover_the_internal_query() => _provider.Performers.Any(_ => _.FullyQualifiedName.Value == $"{typeof(QueryCandidateReadModel).FullName}.InternalHelper").ShouldBeTrue();
}
