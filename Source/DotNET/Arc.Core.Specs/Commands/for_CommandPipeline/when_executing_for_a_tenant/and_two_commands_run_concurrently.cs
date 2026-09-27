// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_two_commands_run_concurrently : given.a_command_pipeline_and_a_handler_for_command
{
    TenantIdAccessor _accessor;
    TenantId _first;
    TenantId _second;
    TenantId _after;
    TaskCompletionSource _firstStarted;
    TaskCompletionSource _secondStarted;

    void Establish()
    {
        _accessor = new TenantIdAccessor(Substitute.For<ITenantIdResolver>());
        _firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _secondStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(async _ =>
        {
            var first = _accessor.Current == new TenantId("acme");
            (first ? _firstStarted : _secondStarted).SetResult();
            await (first ? _secondStarted : _firstStarted).Task.WaitAsync(TimeSpan.FromSeconds(10));
            return CommandResult.Success(_correlationId);
        });
    }

    async Task Because()
    {
        var results = await Task.WhenAll(Run("acme"), Run("globex"));
        _first = results[0];
        _second = results[1];
        _after = _accessor.Current;

        async Task<TenantId> Run(TenantId tenant)
        {
            using (_accessor.Begin(tenant))
            {
                await _commandPipeline.Execute(_command);
                return _accessor.Current;
            }
        }
    }

    [Fact] void should_keep_the_first_command_in_its_tenant() => _first.ShouldEqual(new TenantId("acme"));
    [Fact] void should_keep_the_second_command_in_its_tenant() => _second.ShouldEqual(new TenantId("globex"));
    [Fact] void should_leave_the_caller_without_a_selected_tenant() => _after.ShouldEqual(TenantId.NotSet);
}
