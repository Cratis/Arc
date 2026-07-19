// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_RequiredConceptFilter.when_executing;

public class and_both_event_source_keys_are_null : given.a_required_concept_filter
{
    CommandResult _result;

    async Task Because() => _result = await Execute(new CommandWithPrimaryAndSecondaryEventSourceKeys(null!, null!));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_point_at_the_secondary_key() => _result.ValidationResults.ShouldContain(validationResult => validationResult.Members.Contains(nameof(CommandWithPrimaryAndSecondaryEventSourceKeys.SecondaryId)));
    [Fact] void should_not_point_at_the_resolved_key() => _result.ValidationResults.ShouldNotContain(validationResult => validationResult.Members.Contains(nameof(CommandWithPrimaryAndSecondaryEventSourceKeys.Id)));
}
