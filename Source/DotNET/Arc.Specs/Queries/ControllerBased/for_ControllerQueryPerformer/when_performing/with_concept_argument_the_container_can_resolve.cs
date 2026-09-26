// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Concepts;
using Cratis.Execution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ControllerBased.for_ControllerQueryPerformer.when_performing;

public class with_concept_argument_the_container_can_resolve : Specification
{
    ControllerQueryPerformer _performer;
    ServiceProvider _services;
    QueryContext _context;
    object? _result;

    void Establish()
    {
        // Self-binding registers every concrete type, concepts included, so the real container reports the concept
        // as a service even though it cannot construct one from a bare decimal.
        _services = new ServiceCollection()
            .AddTransient<Rate>()
            .BuildServiceProvider();

        var descriptor = new ControllerActionDescriptor
        {
            ActionName = nameof(RateController.ByRate),
            ControllerName = nameof(RateController),
            ControllerTypeInfo = typeof(RateController).GetTypeInfo(),
            MethodInfo = typeof(RateController).GetMethod(nameof(RateController.ByRate))!
        };

        _performer = new ControllerQueryPerformer(descriptor, _services.GetRequiredService<IServiceProviderIsService>(), Substitute.For<IAuthorizationEvaluator>());
        _context = new QueryContext(
            _performer.FullyQualifiedName,
            CorrelationId.New(),
            Paging.NotPaged,
            Sorting.None,
            new QueryArguments { ["rate"] = "12.5" },
            [_services]);
    }

    async Task Because() => _result = await _performer.Perform(_context);

    void Destroy() => _services.Dispose();

    [Fact] void should_bind_the_concept_from_the_request() => ((RateLookup)_result!).Value.ShouldEqual(12.5m);
    [Fact] void should_expose_the_concept_as_a_query_parameter() => _performer.Parameters.Any(p => p.Name == "rate" && p.Type == typeof(Rate)).ShouldBeTrue();

    public record Rate(decimal Value) : ConceptAs<decimal>(Value);

    public record RateLookup(decimal Value);

    [Route("/api/rates")]
    public class RateController : ControllerBase
    {
        [HttpGet("by-rate")]
        public RateLookup ByRate(Rate rate) => new(rate.Value);
    }
}
