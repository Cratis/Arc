// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Concepts;
using Cratis.Execution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ControllerBased.for_ControllerQueryPerformer.when_performing;

public class with_missing_required_concept_argument : Specification
{
    ControllerQueryPerformer _performer;
    ServiceProvider _services;
    QueryContext _context;
    MissingArgumentForQuery _exception;

    void Establish()
    {
        _services = new ServiceCollection().AddSingleton(new Rate(42m)).BuildServiceProvider();
        RateController.WasCalled = false;
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

    async Task Because() => _exception = await Catch.Exception(PerformQuery) as MissingArgumentForQuery;

    async Task PerformQuery() => await _performer.Perform(_context);

    void Destroy() => _services.Dispose();

    [Fact] void should_reject_the_missing_concept() => _exception.ShouldNotBeNull();
    [Fact] void should_name_the_missing_argument() => _exception.ParameterName.ShouldEqual("rate");
    [Fact] void should_expose_a_required_query_parameter() => _performer.Parameters.Any(p => p.Name == "rate" && p.IsRequired).ShouldBeTrue();
    [Fact] void should_not_call_the_action() => RateController.WasCalled.ShouldBeFalse();

    public record Rate(decimal Value) : ConceptAs<decimal>(Value);

    [Route("/api/required-rates")]
    public class RateController : ControllerBase
    {
        public static bool WasCalled { get; set; }

        [HttpGet]
        public decimal ByRate(Rate rate)
        {
            WasCalled = true;
            return rate.Value;
        }
    }
}
