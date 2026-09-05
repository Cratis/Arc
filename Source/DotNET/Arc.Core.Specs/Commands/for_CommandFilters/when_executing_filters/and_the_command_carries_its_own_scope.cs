// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

/// <summary>
/// A filter is resolved from the scope the command runs in, not from the provider that constructed this singleton,
/// so a filter depending on a scoped service is created in the scope rather than in the root — where the container
/// refuses to create it once scope validation is on.
/// </summary>
public class and_the_command_carries_its_own_scope : Specification
{
    CommandFilters _commandFilters;
    ICommandFilter _filterFromTheScope;
    ICommandFilter _filterFromTheRoot;
    CommandContext _context;
    System.Diagnostics.ActivitySource _activitySource;

    void Establish()
    {
        _filterFromTheScope = Substitute.For<ICommandFilter>();
        _filterFromTheRoot = Substitute.For<ICommandFilter>();

        var scope = Substitute.For<IServiceProvider>();
        scope.GetService(typeof(IInstancesOf<ICommandFilter>))
            .Returns(new KnownInstancesOf<ICommandFilter>([_filterFromTheScope]));

        _context = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new(), ServiceProvider: scope);

        var activitySource = Substitute.For<IActivitySource<CommandFilters>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        activitySource.ActualSource.Returns(_activitySource);
        _commandFilters = new(new KnownInstancesOf<ICommandFilter>([_filterFromTheRoot]), activitySource);
    }

    void Destroy() => _activitySource.Dispose();

    async Task Because() => await _commandFilters.OnExecution(_context);

    [Fact] void should_run_the_filter_from_the_scope() => _filterFromTheScope.Received(1).OnExecution(_context);
    [Fact] void should_not_run_the_filter_from_the_root() => _filterFromTheRoot.DidNotReceive().OnExecution(Arg.Any<CommandContext>());
}
