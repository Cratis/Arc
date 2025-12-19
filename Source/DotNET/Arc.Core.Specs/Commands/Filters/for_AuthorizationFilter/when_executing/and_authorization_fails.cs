// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_AuthorizationFilter.when_executing;

public class and_authorization_fails : given.an_authorization_filter
{
    CommandResult _result;
    object _command;

    void Establish()
    {
        _command = new();
        _context = new CommandContext(_correlationId, typeof(object), _command, [], new());
        _authorizationHelper.IsAuthorized(typeof(object)).Returns(false);
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_call_authorization_helper_with_command_type() => _authorizationHelper.Received(1).IsAuthorized(typeof(object));
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
}