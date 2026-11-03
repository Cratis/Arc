// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Identity;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_a_validator_reads_the_snapshot : given.a_command_pipeline_and_a_handler_for_command
{
    class ReadingValidator : AbstractValidator<string>
    {
        public static TenantId Tenant;
        public static CausingIdentity CausedBy;

        public ReadingValidator() => RuleFor(_ => _).Custom((_, context) =>
        {
            Tenant = context.GetTenant();
            CausedBy = context.GetCausedBy();
        });
    }

    CommandResult _result;

    void Establish()
    {
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("tenant-1");
        var accessor = new TenantIdAccessor(resolver);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1"), new Claim("name", "User")], "test"));
        var principals = Substitute.For<ICurrentPrincipalAccessor>();
        principals.Current.Returns(user);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(principals);
        _serviceProvider.GetService(typeof(TenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdResolver)).Returns(resolver);

        var invoker = new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(async _ =>
        {
            await invoker.Invoke("value", new ReadingValidator(), string.Empty);
            return CommandResult.Success(_correlationId);
        });
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_give_the_validator_the_tenant() => ReadingValidator.Tenant.ShouldEqual(new TenantId("tenant-1"));
    [Fact] void should_give_the_validator_the_caller() => ReadingValidator.CausedBy.Subject.Value.ShouldEqual("user-1");
    [Fact] void should_leave_no_snapshot_behind() => OperationSnapshot.Current.ShouldBeNull();
}
