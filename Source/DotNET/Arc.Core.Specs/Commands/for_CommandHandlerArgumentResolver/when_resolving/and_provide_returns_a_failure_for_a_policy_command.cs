// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_provide_returns_a_failure_for_a_policy_command : given.a_command_handler_argument_resolver
{
    [Command]
    [BlockOnValidationSeverity(ValidationResultSeverity.Information)]
    record CommandWithPolicy;

    CommandHandlerArgumentResolution _result;

    void Establish()
    {
        var policy = CommandValidationResults.ForCommand(typeof(CommandWithPolicy), ValidationResultSeverity.Error);
        _context = new(_correlationId, typeof(CommandWithPolicy), new CommandWithPolicy(), [], new(), policy.AllowedSeverity)
        {
            BlockUnknownValidationSeverity = policy.BlockUnknown
        };
        ProvideReturns(ValidationResult.Information("Provide rejected"));
    }

    async Task Because() => _result = await _resolver.Resolve(_handler, _context, _serviceProvider, ValidationResultSeverity.Error);

    [Fact] void should_short_circuit_before_building_handler_arguments() => _result.IsShortCircuited.ShouldBeTrue();
    [Fact] void should_keep_the_provided_information_failure() => _result.ControlResult.ValidationResults.Single().Message.ShouldEqual("Provide rejected");
    [Fact] void should_keep_the_provided_severity() => _result.ControlResult.ValidationResults.Single().Severity.ShouldEqual(ValidationResultSeverity.Information);
}
