// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_ReadModelDoesNotExistForCommand;

/// <summary>
/// The command never reached its rules: the read model a validator depends on could not be loaded, so the pipeline
/// rejected before any rule ran. Reported as an ordinary rejection, that is a false green - neuter every rule the
/// slice has and its specs still pass, because this is what rejects in their place.
/// </summary>
public class when_the_pipeline_cannot_load_the_read_model : Specification
{
    ValidationResult _doesNotExist;
    ValidationResult _noIdentifier;

    void Because()
    {
        _doesNotExist = new ReadModelDoesNotExistForCommand(typeof(object)).ValidationResult;
        _noIdentifier = new UnableToResolveReadModelFromCommandContext(typeof(object)).ValidationResult;
    }

    [Fact] void should_not_pass_a_missing_read_model_off_as_a_rule_rejection() => _doesNotExist.Reason.ShouldEqual(ValidationResultReason.DependencyUnavailable);
    [Fact] void should_not_pass_a_missing_identifier_off_as_a_rule_rejection() => _noIdentifier.Reason.ShouldEqual(ValidationResultReason.DependencyUnavailable);
}
