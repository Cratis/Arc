// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.Filters;
using Cratis.Arc.Http;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

public class and_authorization_denies_a_caller_whose_command_is_also_invalid : Specification
{
    CommandFilters _commandFilters;
    RecordingValidationFilter _validationFilter;
    CommandContext _context;
    CommandResult _result;
    System.Diagnostics.ActivitySource _activitySource;

    void Establish()
    {
        _context = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new());

        var authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        authorizationEvaluator.IsAuthorized(Arg.Any<Type>()).Returns(false);
        var authorizationFilter = new AuthorizationFilter(authorizationEvaluator);

        _validationFilter = new RecordingValidationFilter();

        // The validation filter is registered first on purpose: authorization must still be decided first (by Order),
        // so a forbidden caller whose command is also invalid gets a clean 403 and the validation filter never runs.
        var filters = new List<ICommandFilter> { _validationFilter, authorizationFilter };
        var commandFiltersActivitySource = Substitute.For<IActivitySource<CommandFilters>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        commandFiltersActivitySource.ActualSource.Returns(_activitySource);
        _commandFilters = new CommandFilters(new KnownInstancesOf<ICommandFilter>(filters), commandFiltersActivitySource);
    }

    void Destroy() => _activitySource.Dispose();

    async Task Because() => _result = await _commandFilters.OnExecution(_context);

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_map_to_forbidden_not_bad_request() => EndpointRouteHelper.GetStatusCode(_result.IsSuccess, _result.IsAuthorized, _result.IsValid).ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_not_run_the_validation_filter() => _validationFilter.WasCalled.ShouldBeFalse();
    [Fact] void should_not_leak_any_validation_detail() => _result.ValidationResults.ShouldBeEmpty();

    class RecordingValidationFilter : ICommandFilter
    {
        public bool WasCalled { get; private set; }

        public Task<CommandResult> OnExecution(CommandContext context)
        {
            WasCalled = true;
            return Task.FromResult(new CommandResult
            {
                CorrelationId = context.CorrelationId,
                ValidationResults = [ValidationResult.Error("name is required")]
            });
        }
    }
}
