// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

public class with_command_having_indexer_property : given.a_fluent_validation_filter
{
    CommandResult _result;
    Exception _exception;

    void Establish()
    {
        var command = new CommandWithIndexer(new ThingWithIndexer());
        _context = new CommandContext(_correlationId, typeof(CommandWithIndexer), command, [], new());
        _discoverableValidators.TryGet(Arg.Any<Type>(), out Arg.Any<IValidator>()).Returns(false);
    }

    async Task Because() => _exception = await Catch.Exception(async () => _result = await _filter.OnExecution(_context));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();

    record CommandWithIndexer(ThingWithIndexer Thing);

    /// <summary>
    /// A non-enumerable type carrying an indexer — reflecting GetValue(instance) over it without index
    /// arguments would throw "Parameter count mismatch" if the filter did not skip indexers.
    /// </summary>
    public class ThingWithIndexer
    {
        public int this[int index] => index;

        public string Name => "ok";
    }
}
