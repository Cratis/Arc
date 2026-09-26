// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_validating_for_a_tenant : given.a_command_pipeline_and_a_handler_for_command
{
    TenantIdAccessor _accessor;
    TenantId _tenantAtScopeCreation;
    TenantId _tenantInValidation;

    void Establish()
    {
        _accessor = new TenantIdAccessor(Substitute.For<ITenantIdResolver>());
        _serviceScopeFactory.CreateScope().Returns(_ =>
        {
            _tenantAtScopeCreation = _accessor.Current;
            return _serviceScope;
        });
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(_ =>
        {
            _tenantInValidation = _accessor.Current;
            return CommandResult.Success(_correlationId);
        });
    }

    async Task Because()
    {
        using (_accessor.Begin("acme"))
        {
            await _commandPipeline.Validate(_command);
        }
    }

    [Fact] void should_select_the_tenant_before_creating_the_validation_scope() => _tenantAtScopeCreation.ShouldEqual(new TenantId("acme"));
    [Fact] void should_use_the_tenant_in_validation_filters() => _tenantInValidation.ShouldEqual(new TenantId("acme"));
}
