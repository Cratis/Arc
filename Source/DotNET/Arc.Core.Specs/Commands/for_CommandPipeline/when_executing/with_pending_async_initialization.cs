// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class with_pending_async_initialization : given.a_pipeline_with_async_scopes
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _filtersRanBeforeInitialization;
    CommandContext _context;

    void Establish()
    {
        _commandContextValuesBuilder.Build(_command).Returns(new CommandContextValues { ["built"] = true });
        _asyncScope.BeginAsync(Arg.Any<CommandContext>()).Returns(async call =>
        {
            _context = call.Arg<CommandContext>();
            _entered.SetResult();
            await _release.Task;
            _context.Values["initialized"] = true;
        });
    }

    async Task Because()
    {
        var execution = _commandPipeline.Execute(_command, _serviceProvider);
        try
        {
            await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            _filtersRanBeforeInitialization = _calls.Contains("filters");
        }
        finally
        {
            _release.TrySetResult();
            _result = await execution;
        }
    }

    [Fact] void should_initialize_with_built_context_values() => _context.Values["built"].ShouldEqual(true);
    [Fact] void should_wait_before_running_filters() => _filtersRanBeforeInitialization.ShouldBeFalse();
    [Fact] void should_share_built_context_values_with_filters() => _commandFilters.Received(1).OnExecution(Arg.Is<CommandContext>(context => context.Values.ContainsKey("initialized") && ReferenceEquals(context.Values, _context.Values)));
}
