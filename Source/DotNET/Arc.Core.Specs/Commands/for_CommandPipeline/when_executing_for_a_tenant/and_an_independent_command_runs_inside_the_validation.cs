// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_an_independent_command_runs_inside_the_validation : given.a_command_pipeline_and_a_handler_for_command
{
    readonly List<(string Phase, TenantId Tenant, string Subject)> _seen = [];
    IDisposable _boundary;

    void Establish()
    {
        _boundary = TenantIdAccessor.Independent.BeginRequest();
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("outer-tenant");
        var accessor = new TenantIdAccessor(resolver);
        var outer = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "outer-user")], "test"));
        var inner = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "inner-user")], "test"));
        var principals = Substitute.For<ICurrentPrincipalAccessor>();
        principals.Current.Returns(outer);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(principals);
        _serviceProvider.GetService(typeof(TenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdResolver)).Returns(resolver);

        var depth = 0;
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(async _ =>
        {
            if (depth++ == 0)
            {
                _seen.Add(("outer-before", OperationSnapshot.Current!.Tenant, OperationSnapshot.Current.CausedBy.Subject.Value));
                ITenantScope scope = accessor;
                using (scope.Begin("inner-tenant"))
                {
                    principals.Current.Returns(inner);
                    await _commandPipeline.Execute(_command);
                    principals.Current.Returns(outer);
                }

                _seen.Add(("outer-after", OperationSnapshot.Current!.Tenant, OperationSnapshot.Current.CausedBy.Subject.Value));
            }
            else
            {
                _seen.Add(("inner", OperationSnapshot.Current!.Tenant, OperationSnapshot.Current.CausedBy.Subject.Value));
            }

            return CommandResult.Success(_correlationId);
        });
    }

    void Destroy() => _boundary.Dispose();

    async Task Because() => await _commandPipeline.Execute(_command);

    [Fact] void should_give_the_outer_operation_its_tenant_and_caller() => _seen.Single(_ => _.Phase == "outer-before").ShouldEqual(("outer-before", new TenantId("outer-tenant"), "outer-user"));
    [Fact] void should_give_the_inner_operation_a_fresh_frame() => _seen.Single(_ => _.Phase == "inner").ShouldEqual(("inner", new TenantId("inner-tenant"), "inner-user"));
    [Fact] void should_restore_the_outer_frame() => _seen.Single(_ => _.Phase == "outer-after").ShouldEqual(("outer-after", new TenantId("outer-tenant"), "outer-user"));
}
