// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_provide_returns_validation_with_a_property_name : given.a_command_handler_argument_resolver
{
    CommandHandlerArgumentResolution _result;

    void Establish() => ProvideReturns(new RejectedCommand("Reserved").Provide());

    async Task Because() => _result = await Resolve();

    [Fact] void should_short_circuit() => _result.IsShortCircuited.ShouldBeTrue();
    [Fact] void should_report_the_client_field() => _result.ControlResult.ValidationResults.Single().Members.ShouldContainOnly("name");

    record RejectedCommand(string Name)
    {
        public ValidationResult Provide() => ValidationResult.Error("Reserved", [nameof(Name)]);
    }
}
