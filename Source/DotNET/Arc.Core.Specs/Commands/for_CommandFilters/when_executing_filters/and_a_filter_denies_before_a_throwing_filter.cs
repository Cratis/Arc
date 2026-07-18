// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http;
using Cratis.Execution;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

public class and_a_filter_denies_before_a_throwing_filter : Specification
{
    CommandFilters _commandFilters;
    ICommandFilter _authorizationFilter;
    ICommandFilter _validationFilter;
    CommandContext _context;
    CommandResult _result;
    System.Diagnostics.ActivitySource _activitySource;

    void Establish()
    {
        _authorizationFilter = Substitute.For<ICommandFilter>();
        _validationFilter = Substitute.For<ICommandFilter>();
        _context = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new());

        _authorizationFilter.OnExecution(_context).Returns(Task.FromResult(CommandResult.Unauthorized(_context.CorrelationId, "forbidden")));

        // A later filter (e.g. a validator dereferencing a null concept) would throw if it ran — the chain
        // must short-circuit on the authorization denial before reaching it, so the throw never happens.
        _validationFilter.OnExecution(_context).Returns<Task<CommandResult>>(_ => throw new InvalidOperationException("validator dereferenced a null concept"));

        var filters = new List<ICommandFilter> { _authorizationFilter, _validationFilter };
        var commandFiltersActivitySource = Substitute.For<IActivitySource<CommandFilters>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        commandFiltersActivitySource.ActualSource.Returns(_activitySource);
        _commandFilters = new CommandFilters(new KnownInstancesOf<ICommandFilter>(filters), commandFiltersActivitySource);
    }

    void Destroy() => _activitySource.Dispose();

    async Task Because() => _result = await _commandFilters.OnExecution(_context);

    [Fact] void should_call_the_authorization_filter() => _authorizationFilter.Received(1).OnExecution(_context);
    [Fact] void should_not_call_the_throwing_filter() => _validationFilter.DidNotReceive().OnExecution(_context);
    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_carry_any_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_map_to_forbidden_and_not_internal_server_error() => EndpointRouteHelper.GetStatusCode(_result.IsSuccess, _result.IsAuthorized, _result.IsValid).ShouldEqual(HttpStatusCode.Forbidden);
}
