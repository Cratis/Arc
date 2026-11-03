// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_the_selected_principal_is_not_mutated : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    string _subject;
    TenantId _tenant;
    IDisposable _boundary;

    void Establish()
    {
        _boundary = TenantIdAccessor.Independent.BeginRequest();
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("tenant-1");
        var accessor = new TenantIdAccessor(resolver);
        var ambient = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "ambient")], "test"));
        var selected = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "selected")], "test"));
        var principals = Substitute.For<ICurrentPrincipalAccessor>();
        principals.Current.Returns(ambient);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(principals);
        _serviceProvider.GetService(typeof(TenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdResolver)).Returns(resolver);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(call =>
        {
            call.Arg<CommandContext>().AuthorizedPrincipal = selected;
            _subject = OperationSnapshot.Current!.CausedBy.Subject.Value;
            _tenant = OperationSnapshot.Current.Tenant;

            // Not a success, so the pipeline stops before it requires prepared authorization for the selected principal.
            return CommandResult.Error(_correlationId, new InvalidOperationException("filter failed"));
        });
    }

    void Destroy() => _boundary.Dispose();

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_not_reject_as_unauthorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_read_the_selected_caller() => _subject.ShouldEqual("selected");
    [Fact] void should_read_the_tenant() => _tenant.ShouldEqual(new TenantId("tenant-1"));
}
