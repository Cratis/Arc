// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_discovering_query_candidates : Specification
{
    readonly QueryScenario<ScenarioQueryCandidates> _scenario = new();
    ServiceProvider _services;
    ScenarioQueryPerformerProviders<ScenarioQueryCandidates> _providers;
    QueryResult _internalResult;
    QueryResult _privateStreamResult;

    async Task Because()
    {
        _services = new ServiceCollection().BuildServiceProvider();
        _providers = new ScenarioQueryPerformerProviders<ScenarioQueryCandidates>(_services);
        _internalResult = await _scenario.Perform("InternalQuery");
        _privateStreamResult = await _scenario.Perform("PrivateStream");
    }

    [Fact] void should_register_only_two_queries() => _providers.Performers.Count().ShouldEqual(2);
    [Fact] void should_register_the_public_query() => _providers.Performers.Any(_ => _.FullyQualifiedName.Value == $"{typeof(ScenarioQueryCandidates).FullName}.PublicQuery").ShouldBeTrue();
    [Fact] void should_register_the_internal_query() => _providers.Performers.Any(_ => _.FullyQualifiedName.Value == $"{typeof(ScenarioQueryCandidates).FullName}.InternalQuery").ShouldBeTrue();
    [Fact] void should_perform_the_internal_query() => ((ScenarioQueryCandidates)_internalResult.Data).Name.ShouldEqual("Internal");
    [Fact] void should_not_treat_a_private_stream_as_a_query() => _privateStreamResult.IsSuccess.ShouldBeFalse();

    void Destroy()
    {
        _scenario.Dispose();
        _services.Dispose();
    }
}
