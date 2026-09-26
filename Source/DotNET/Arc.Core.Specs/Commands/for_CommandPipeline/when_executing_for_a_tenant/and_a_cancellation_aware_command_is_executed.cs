// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_a_cancellation_aware_command_is_executed : given.a_command_pipeline_and_a_handler_for_command
{
    TenantIdAccessor _accessor;
    TenantId _atScopeCreation;
    TenantId _inFilter;

    void Establish()
    {
        _accessor = new TenantIdAccessor(Substitute.For<ITenantIdResolver>());
        _serviceScopeFactory.CreateScope().Returns(_ =>
        {
            _atScopeCreation = _accessor.Current;
            return _serviceScope;
        });
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(_ =>
        {
            _inFilter = _accessor.Current;
            return CommandResult.Success(_correlationId);
        });
    }

    async Task Because()
    {
        using (_accessor.Begin("acme"))
        {
            ICommandPipelineWithCancellation pipeline = _commandPipeline;
            await pipeline.Execute(_command, null, CancellationToken.None);
        }
    }

    [Fact] void should_select_the_tenant_before_creating_the_di_scope() => _atScopeCreation.ShouldEqual(new TenantId("acme"));
    [Fact] void should_use_the_tenant_during_execution() => _inFilter.ShouldEqual(new TenantId("acme"));
}
