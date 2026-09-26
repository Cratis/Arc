// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_the_handler_throws : given.a_command_pipeline_and_a_handler_for_command
{
    TenantIdAccessor _accessor;
    TenantId _tenantDuringHandler;
    TenantId _tenantAfter;
    CommandResult _result;

    void Establish()
    {
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("original");
        _accessor = new TenantIdAccessor(resolver);
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_ =>
        {
            _tenantDuringHandler = _accessor.Current;
            throw new Exception("handler failed");
        });
    }

    async Task Because()
    {
        using (_accessor.Begin("acme"))
        {
            _result = await _commandPipeline.Execute(_command);
        }

        _tenantAfter = _accessor.Current;
    }

    [Fact] void should_use_the_selected_tenant_during_the_handler() => _tenantDuringHandler.ShouldEqual(new TenantId("acme"));
    [Fact] void should_restore_the_previous_tenant_after_failure() => _tenantAfter.ShouldEqual(new TenantId("original"));
    [Fact] void should_report_the_handler_failure() => _result.IsSuccess.ShouldBeFalse();
}
