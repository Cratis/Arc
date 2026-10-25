// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope.when_the_action_executes_a_pipeline_command;

public class and_read_protected_commands_overlap : given.a_controller_invoking_the_pipeline
{
    readonly TaskCompletionSource _bothEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<IUnitOfWork> _units = [];
    CommandResult[] _results;
    int _entered;

    void Establish()
    {
        _handle = async _ =>
        {
            lock (_units) _units.Add(_manager.Current);
            if (Interlocked.Increment(ref _entered) == 2) _bothEntered.SetResult();
            await _bothEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return null;
        };
        _action = async () => _results = await Task.WhenAll(Execute(new EnforcingCommand()), Execute(new EnforcingCommand()));
    }

    Task Because() => Invoke();

    [Fact] void should_execute_both_commands_without_nested_read_protection_refusals() => _results.All(_ => _.IsSuccess).ShouldBeTrue();
    [Fact] void should_give_each_command_a_distinct_unit() => _units.Distinct().Count().ShouldEqual(2);
    [Fact] void should_not_use_the_controller_unit() => _units.ShouldNotContain(_controllerUnit);
    [Fact] void should_keep_the_controller_successful() => ControllerResult.IsSuccess.ShouldBeTrue();
}
