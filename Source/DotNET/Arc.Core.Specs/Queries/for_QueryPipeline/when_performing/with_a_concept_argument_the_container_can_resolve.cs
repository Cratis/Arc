// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Concepts;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_concept_argument_the_container_can_resolve : given.a_query_pipeline
{
    ServiceProvider _services;
    object? _renderedData;
    QueryResult _result;

    void Establish()
    {
        // Self-binding registers every concrete type, concepts included, so the real container reports the concept
        // as a service even though it cannot construct one from a bare decimal.
        _services = new ServiceCollection()
            .AddTransient<Rate>()
            .BuildServiceProvider();
        _serviceProvider = _services;

        var method = typeof(RateLookup).GetMethod(nameof(RateLookup.ByRate))!;
        var performer = new ModelBoundQueryPerformer(
            typeof(RateLookup),
            typeof(RateLookup).FullName!,
            method,
            _services.GetRequiredService<IServiceProviderIsService>(),
            Substitute.For<IAuthorizationEvaluator>());

        _queryPerformerProviders.TryGetPerformersFor(performer.FullyQualifiedName, out var _).Returns(callInfo =>
        {
            callInfo[1] = performer;
            return true;
        });

        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(QueryResult.Success(_correlationId));
        _queryRenderers.Render(Arg.Any<FullyQualifiedQueryName>(), Arg.Do<object>(data => _renderedData = data), Arg.Any<IServiceProvider>())
            .Returns(callInfo => new QueryRendererResult(1, callInfo.ArgAt<object>(1)));
    }

    async Task Because() => _result = await _pipeline.Perform(
        $"{typeof(RateLookup).FullName}.{nameof(RateLookup.ByRate)}",
        new QueryArguments { ["RATE"] = "12.5" },
        Paging.NotPaged,
        Sorting.None,
        _serviceProvider);

    void Destroy() => _services.Dispose();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_bind_the_concept_from_the_request() => ((RateLookup)_renderedData!).Value.ShouldEqual(12.5m);

    public record Rate(decimal Value) : ConceptAs<decimal>(Value);

    public record RateLookup(decimal Value)
    {
        public static RateLookup ByRate(Rate rate) => new(rate.Value);
    }
}
