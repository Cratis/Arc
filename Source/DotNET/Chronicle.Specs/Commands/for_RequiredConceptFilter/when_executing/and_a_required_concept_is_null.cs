// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_RequiredConceptFilter.when_executing;

public class and_a_required_concept_is_null : given.a_required_concept_filter
{
    CommandResult _result;

    async Task Because() => _result = await Execute(new CommandWithRequiredConcept(null!));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_carry_any_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_point_at_the_missing_property() => _result.ValidationResults.ShouldContain(validationResult => validationResult.Members.Contains(nameof(CommandWithRequiredConcept.Name)));
}
