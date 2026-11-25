// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Execution;

namespace Cratis.Arc.Availability.for_AvailabilityInvocationIdentity;

public class when_contexts_are_copied_or_compared : Specification
{
    CommandContext _command;
    CommandContext _equalCommand;
    QueryContext _query;
    QueryContext _equalQuery;
    AvailabilityInvocation _commandInvocation;
    AvailabilityInvocation _queryInvocation;
    AvailabilityInvocation _copiedCommand;
    AvailabilityInvocation _copiedQuery;

    void Establish()
    {
        _command = new CommandContext(CorrelationId.New(), typeof(string), "bound", [], new CommandContextValues());
        _equalCommand = new(_command.CorrelationId, _command.Type, _command.Command, _command.Dependencies, _command.Values) { GeneratedValues = _command.GeneratedValues };
        _query = new("Test.Query", CorrelationId.New(), Paging.NotPaged, Sorting.None, new QueryArguments());
        _equalQuery = new(_query.Name, _query.CorrelationId, _query.Paging, _query.Sorting, _query.Arguments);
    }

    void Because()
    {
        _commandInvocation = _command.AvailabilityInvocation;
        _queryInvocation = _query.InvocationFor(typeof(string));
        _copiedCommand = (_command with { Dependencies = [new object()] }).AvailabilityInvocation;
        _copiedQuery = (_query with { Dependencies = [new object()] }).InvocationFor(typeof(string));
    }

    [Fact] void should_not_change_command_value_equality() => _command.ShouldEqual(_equalCommand);
    [Fact] void should_not_change_query_value_equality() => _query.ShouldEqual(_equalQuery);
    [Fact] void should_share_command_identity_with_its_copy() => _copiedCommand.ShouldBeSame(_commandInvocation);
    [Fact] void should_share_query_identity_with_its_copy() => _copiedQuery.ShouldBeSame(_queryInvocation);
    [Fact] void should_keep_an_independent_command_distinct() => ReferenceEquals(_commandInvocation, _equalCommand.AvailabilityInvocation).ShouldBeFalse();
    [Fact] void should_keep_an_independent_query_distinct() => ReferenceEquals(_queryInvocation, _equalQuery.InvocationFor(typeof(string))).ShouldBeFalse();
    [Fact] void should_not_recalculate_a_manual_context_receipt() => _commandInvocation.ReceivedAt.ShouldEqual(default);
}
