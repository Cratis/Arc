// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_validating;

public class with_collection_properties : given.a_fluent_validation_filter
{
    CommandResult _result;
    CommandWithCollection _command;

    void Establish()
    {
        var items = new[] { new Item("Item1"), new Item("Item2") };
        _command = new CommandWithCollection("CommandName", items);
        _context = new CommandContext(_correlationId, typeof(CommandWithCollection), _command, [], new());
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_have_validation_results() => _result.ValidationResults.ShouldBeEmpty();

    record CommandWithCollection(string Name, Item[] Items);
    record Item(string Name);
}