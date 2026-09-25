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
    bool _runningInner;

    void Establish()
    {
        _accessor = new TenantIdAccessor(Substitute.For<ITenantIdResolver>());
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(async _ =>
        {
            if (_runningInner)
            {
                _inner = _accessor.Current;
            }
            else
            {
                _outerBefore = _accessor.Current;
                _runningInner = true;
                using (_accessor.Begin("inner"))
                {
                    await _commandPipeline.Execute(_command);
                }

                _outerAfter = _accessor.Current;
            }

            return CommandResult.Success(_correlationId);
        });
    }

    async Task Because()
    {
        using (_accessor.Begin("outer"))
        {
            await _commandPipeline.Execute(_command);
        }
    }

    [Fact] void should_use_the_inner_tenant_inside_the_inner_command() => _inner.ShouldEqual(new TenantId("inner"));
    [Fact] void should_restore_the_outer_tenant_after_the_inner_command() => _outerAfter.ShouldEqual(new TenantId("outer"));
    [Fact] void should_use_the_outer_tenant_in_the_outer_command() => _outerBefore.ShouldEqual(new TenantId("outer"));
}
