// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandValidationResults.when_resolving_an_assembly_blocking_policy;

public class and_the_base_type_declares_one : given.a_command_policy
{
    void Establish() => _commandType = DeclareCommand(ValidationResultSeverity.Information, baseType: DeclareCommand(null, ValidationResultSeverity.Warning));
    void Because() => _policy = CommandValidationResults.ForCommand(_commandType, ValidationResultSeverity.Error);

    [Fact] void should_prefer_the_base_type_policy() => _policy.AllowedSeverity.ShouldEqual(ValidationResultSeverity.Information);
}
