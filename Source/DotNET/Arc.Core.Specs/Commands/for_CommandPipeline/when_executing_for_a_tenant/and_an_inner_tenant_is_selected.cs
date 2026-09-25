// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_an_inner_tenant_is_selected : given.a_command_pipeline_and_a_handler_for_command
{
    TenantIdAccessor _accessor;
    TenantId _outerBefore;
    TenantId _inner;
    TenantId _outerAfter;

    void Establish()
    {
        _accessor = new TenantIdAccessor(Substitute.For<ITenantIdResolver>());
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(_ =>
        {
            _outerBefore = _accessor.Current;
            using (TenantIdAccessor.UseTenant(new TenantId("inner")))
            {
                _inner = _accessor.Current;
            }

            _outerAfter = _accessor.Current;
            return CommandResult.Success(_correlationId);
        });
    }

    async Task Because() => await _commandPipeline.Execute(_command, _serviceProvider, new TenantId("outer"));

    [Fact] void should_use_the_inner_tenant_inside_the_inner_scope() => _inner.ShouldEqual(new TenantId("inner"));
    [Fact] void should_restore_the_outer_tenant_after_the_inner_scope() => _outerAfter.ShouldEqual(_outerBefore);
    [Fact] void should_use_the_outer_tenant_in_the_command() => _outerBefore.ShouldEqual(new TenantId("outer"));
}
