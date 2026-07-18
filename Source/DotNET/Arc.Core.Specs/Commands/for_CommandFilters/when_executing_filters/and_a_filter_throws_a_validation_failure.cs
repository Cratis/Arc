// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

public class and_a_filter_throws_a_validation_failure : Specification
{
    CommandFilters _commandFilters;
    ICommandFilter _filter;
    CommandContext _context;
    CommandResult _result;
    System.Diagnostics.ActivitySource _activitySource;

    void Establish()
    {
        _filter = Substitute.For<ICommandFilter>();
        _context = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new());

        // A validator whose read-model dependency cannot be resolved (no event source id) throws an IValidationFailure
        // during construction — which surfaces here as a throwing filter. It must become a 400, not a 500.
        _filter.OnExecution(_context).Returns<Task<CommandResult>>(_ => throw new TheValidationFailure());

        var filters = new List<ICommandFilter> { _filter };
        var commandFiltersActivitySource = Substitute.For<IActivitySource<CommandFilters>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        commandFiltersActivitySource.ActualSource.Returns(_activitySource);
        _commandFilters = new CommandFilters(new KnownInstancesOf<ICommandFilter>(filters), commandFiltersActivitySource);
    }

    void Destroy() => _activitySource.Dispose();

    async Task Because() => _result = await _commandFilters.OnExecution(_context);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_carry_any_exception_detail() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_surface_the_validation_message() => _result.ValidationResults.ShouldContain(vr => vr.Message == "missing identifier");
    [Fact] void should_map_to_bad_request_not_internal_server_error() => EndpointRouteHelper.GetStatusCode(_result.IsSuccess, _result.IsAuthorized, _result.IsValid).ShouldEqual(HttpStatusCode.BadRequest);

    class TheValidationFailure : Exception, IValidationFailure
    {
        public ValidationResult ValidationResult { get; } = ValidationResult.Error("missing identifier");
    }
}
