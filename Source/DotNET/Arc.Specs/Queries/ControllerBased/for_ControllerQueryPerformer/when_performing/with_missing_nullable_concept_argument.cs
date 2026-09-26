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

public class with_missing_nullable_concept_argument : Specification
{
    ControllerQueryPerformer _performer;
    ServiceProvider _services;
    QueryContext _context;
    object? _result;

    void Establish()
    {
        _services = new ServiceCollection().AddSingleton(new Rate(42m)).BuildServiceProvider();
        var descriptor = new ControllerActionDescriptor
        {
            ActionName = nameof(RateController.ByRate),
            ControllerName = nameof(RateController),
            ControllerTypeInfo = typeof(RateController).GetTypeInfo(),
            MethodInfo = typeof(RateController).GetMethod(nameof(RateController.ByRate))!
        };
        _performer = new ControllerQueryPerformer(descriptor, _services.GetRequiredService<IServiceProviderIsService>(), Substitute.For<IAuthorizationEvaluator>());
        _context = new QueryContext(_performer.FullyQualifiedName, CorrelationId.New(), Paging.NotPaged, Sorting.None, QueryArguments.Empty, [_services]);
    }

    async Task Because() => _result = await _performer.Perform(_context);

    void Destroy() => _services.Dispose();

    [Fact] void should_pass_null_to_the_action() => _result.ShouldEqual("not supplied");
    [Fact] void should_expose_an_optional_query_parameter() => _performer.Parameters.Any(p => p.Name == "rate" && !p.IsRequired).ShouldBeTrue();

    public record Rate(decimal Value) : ConceptAs<decimal>(Value);

    [Route("/api/optional-rates")]
    public class RateController : ControllerBase
    {
        [HttpGet]
        public string ByRate(Rate? rate) => rate is null ? "not supplied" : "supplied";
    }
}
