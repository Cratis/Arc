// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_the_tenant_was_already_resolved : given.a_command_pipeline_and_a_handler_for_command
{
    TenantIdAccessor _accessor;
    TenantId _observed;
    TenantId _before;
    TenantId _after;

    void Establish()
    {
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("original");
        _accessor = new TenantIdAccessor(resolver);
        _before = _accessor.Current;
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(_ =>
        {
            _observed = _accessor.Current;
            return CommandResult.Success(_correlationId);
        });
    }

    async Task Because()
    {
        ITenantScope tenants = _accessor;
        using (tenants.Begin("explicit"))
        {
            await _commandPipeline.Execute(_command);
        }

        _after = _accessor.Current;
    }

    [Fact] void should_use_the_explicit_tenant_during_execution() => _observed.ShouldEqual(new TenantId("explicit"));
    [Fact] void should_restore_the_previous_tenant_after_disposal() => _after.ShouldEqual(_before);
}
