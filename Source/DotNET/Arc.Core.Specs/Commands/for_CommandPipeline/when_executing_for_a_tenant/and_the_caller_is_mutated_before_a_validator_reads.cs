// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_the_caller_is_mutated_before_a_validator_reads : given.a_command_pipeline_and_a_handler_for_command
{
    class ReadingValidator : AbstractValidator<string>
    {
        public ReadingValidator() => RuleFor(_ => _).Custom((_, context) => context.GetTenant());
    }

    CommandResult _result;

    void Establish()
    {
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("tenant-1");
        var accessor = new TenantIdAccessor(resolver);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "test"));
        var principals = Substitute.For<ICurrentPrincipalAccessor>();
        principals.Current.Returns(user);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(principals);
        _serviceProvider.GetService(typeof(TenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdResolver)).Returns(resolver);

        var invoker = new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(async _ =>
        {
            ((ClaimsIdentity)user.Identity!).AddClaim(new Claim(ClaimTypes.Role, "admin"));

            // A validator that swallows the failure must not be able to hide it.
            try
            {
                await invoker.Invoke("value", new ReadingValidator(), string.Empty);
            }
            catch
            {
            }

            return CommandResult.Success(_correlationId);
        });
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
}
