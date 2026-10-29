// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandValidationResults.when_resolving_an_assembly_blocking_policy;

public class and_only_the_base_types_assembly_declares_one : given.a_command_policy
{
    void Establish() => _commandType = DeclareCommand(null, baseType: DeclareCommand(ValidationResultSeverity.Information));
    void Because() => _policy = CommandValidationResults.ForCommand(_commandType, ValidationResultSeverity.Error);

    [Fact] void should_not_apply_the_base_types_assembly_policy() => _policy.BlockUnknown.ShouldBeFalse();
    [Fact] void should_keep_the_caller_threshold() => _policy.AllowedSeverity.ShouldEqual(ValidationResultSeverity.Error);
}
