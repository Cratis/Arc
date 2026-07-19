// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_RequiredConceptFilter.when_executing;

public class and_the_primary_event_source_key_is_null_with_a_secondary_present : given.a_required_concept_filter
{
    CommandResult _result;

    async Task Because() => _result = await Execute(new CommandWithPrimaryAndSecondaryEventSourceKeys(null!, new CustomerId(Guid.NewGuid())));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
