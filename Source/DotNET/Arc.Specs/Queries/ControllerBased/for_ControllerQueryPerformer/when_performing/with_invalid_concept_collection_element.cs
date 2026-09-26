// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using Cratis.Execution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ControllerBased.for_ControllerQueryPerformer.when_performing;

public class with_invalid_concept_collection_element : Specification
{
    ControllerQueryPerformer _performer;
    ServiceProvider _services;
    QueryContext _context;
    InvalidQueryArgument _exception;

    void Establish()
    {
        _services = new ServiceCollection().BuildServiceProvider();
        RateController.WasCalled = false;
        var descriptor = new ControllerActionDescriptor
        {
            ActionName = nameof(RateController.ByRates),
            ControllerName = nameof(RateController),
            ControllerTypeInfo = typeof(RateController).GetTypeInfo(),
            MethodInfo = typeof(RateController).GetMethod(nameof(RateController.ByRates))!
        };
        _performer = new ControllerQueryPerformer(descriptor, _services.GetRequiredService<IServiceProviderIsService>(), Substitute.For<IAuthorizationEvaluator>());
        _context = new QueryContext(_performer.FullyQualifiedName, CorrelationId.New(), Paging.NotPaged, Sorting.None, new QueryArguments { ["rates"] = "1,bad" }, [_services]);
    }

    async Task Because() => _exception = await Catch.Exception(PerformQuery) as InvalidQueryArgument;

    async Task PerformQuery() => await _performer.Perform(_context);

    void Destroy() => _services.Dispose();

    [Fact] void should_classify_the_collection_as_a_query_argument() => _performer.Parameters.Any(_ => _.Name == "rates").ShouldBeTrue();
    [Fact] void should_reject_the_invalid_element() => _exception.ShouldNotBeNull();
    [Fact] void should_name_the_argument_in_the_validation_result() => _exception.ValidationResult.Members.ShouldContain("rates");
    [Fact] void should_have_a_validation_failure() => (_exception is IValidationFailure).ShouldBeTrue();
    [Fact] void should_report_malformed_request() => _exception.ValidationResult.Reason.ShouldEqual(ValidationResultReason.MalformedRequest);
    [Fact] void should_not_invoke_the_action() => RateController.WasCalled.ShouldBeFalse();

    public record Rate(decimal Value) : ConceptAs<decimal>(Value);

    [Route("/api/invalid-rate-collections")]
    public class RateController : ControllerBase
    {
        public static bool WasCalled { get; set; }

        [HttpGet]
        public decimal ByRates(IEnumerable<Rate> rates)
        {
            WasCalled = true;
            return rates.First().Value;
        }
    }
}
