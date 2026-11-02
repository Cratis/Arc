// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_strict_membership_is_disabled : given.a_command_pipeline_and_a_handler_for_command
{
    class DenyingValidator : AbstractValidator<string>
    {
        /// <summary>Stands in for a validator whose injected accessor denies membership.</summary>
        public DenyingValidator() => RuleFor(_ => _).Custom((_, __) => throw new TenantMembershipDenied());
    }

    CommandResult _result;
    ValidationResultReason[] _reasons = [];

    void Establish()
    {
        var options = new ArcOptions();
        options.Tenancy.StrictValidatorMembership = false;
        _serviceProvider.GetService(typeof(IOptions<ArcOptions>)).Returns(Options.Create(options));

        var invoker = new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(async _ =>
        {
            var results = await invoker.Invoke("value", new DenyingValidator(), string.Empty);
            _reasons = [.. results.Select(result => result.Reason)];
            return CommandResult.Success(_correlationId);
        });
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_stay_a_validator_failure() => _reasons.ShouldContainOnly(ValidationResultReason.ValidatorFailed);
    [Fact] void should_not_be_unauthorized() => _result.IsAuthorized.ShouldBeTrue();
}
