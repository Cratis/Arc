// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_metadata_contains_self_parameter_helpers : Specification
{
    QueryPerformerProvider _provider;

    void Because()
    {
        var types = Substitute.For<ITypes>();
        var registry = Substitute.For<IQueryMetadataRegistry>();
        registry.All.Returns(new[] { "ById", "Copy", "Reduce" }.ToDictionary(name => $"{typeof(QueryCandidateReadModel).FullName}.{name}", _ => typeof(QueryCandidateReadModel)));
        _provider = new(types, registry, Substitute.For<IServiceProviderIsService>(), Substitute.For<IAuthorizationEvaluator>());
    }

    [Fact] void should_recheck_the_runtime_rule_before_registering_metadata() => _provider.Performers.Select(_ => _.FullyQualifiedName.Value).ShouldContainOnly($"{typeof(QueryCandidateReadModel).FullName}.ById");
}
