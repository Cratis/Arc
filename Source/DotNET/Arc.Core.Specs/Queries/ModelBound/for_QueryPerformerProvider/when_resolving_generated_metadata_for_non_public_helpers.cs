// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_resolving_generated_metadata_for_non_public_helpers : Specification
{
    QueryPerformerProvider _provider;
    ITypes _types;
    IQueryMetadataRegistry _registry;
    IServiceProviderIsService _serviceProviderIsService;
    IAuthorizationEvaluator _authorizationEvaluator;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _registry = Substitute.For<IQueryMetadataRegistry>();
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();

        var type = typeof(QueryCandidateReadModel);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(_ => _.ReturnType == type)
            .ToDictionary(_ => $"{type.FullName}.{_.Name}", _ => type);
        _registry.All.Returns(methods);
    }

    void Because() => _provider = new QueryPerformerProvider(_types, _registry, _serviceProviderIsService, _authorizationEvaluator);

    [Fact] void should_resolve_only_two_queries() => _provider.Performers.Count().ShouldEqual(2);
    [Fact] void should_resolve_the_public_query() => _provider.Performers.Any(_ => _.FullyQualifiedName.Value == $"{typeof(QueryCandidateReadModel).FullName}.{nameof(QueryCandidateReadModel.ById)}").ShouldBeTrue();
    [Fact] void should_resolve_the_internal_query() => _provider.Performers.Any(_ => _.FullyQualifiedName.Value == $"{typeof(QueryCandidateReadModel).FullName}.InternalHelper").ShouldBeTrue();
}
