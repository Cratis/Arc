// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.Filters;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

public class given_two_blocking_filters : Specification
{
    protected CommandResult _result;
    CommandContext _context;
    System.Diagnostics.ActivitySource _activitySource;
    IActivitySource<CommandFilters> _commandFiltersActivitySource;

    void Establish()
    {
        _context = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new());
        _commandFiltersActivitySource = Substitute.For<IActivitySource<CommandFilters>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        _commandFiltersActivitySource.ActualSource.Returns(_activitySource);
    }

    void Destroy() => _activitySource.Dispose();

    protected async Task ExecuteWith(params ICommandFilter[] discoveredFilters)
    {
        var commandFilters = new CommandFilters(new KnownInstancesOf<ICommandFilter>(discoveredFilters), _commandFiltersActivitySource);
        _result = await commandFilters.OnExecution(_context);
    }

    protected static Task<CommandResult> Block(CommandContext context, string message) =>
        Task.FromResult(new CommandResult
        {
            CorrelationId = context.CorrelationId,
            ValidationResults = [ValidationResult.Error(message)]
        });

    protected class AFilter : ICommandFilter
    {
        public Task<CommandResult> OnExecution(CommandContext context) => Block(context, "error from A");
    }

    protected class BFilter : ICommandFilter
    {
        public Task<CommandResult> OnExecution(CommandContext context) => Block(context, "error from B");
    }
}
