// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_RequiredConceptFilter.when_executing;

public class and_a_nullable_concept_is_null : given.a_required_concept_filter
{
    CommandResult _result;

    async Task Because() => _result = await Execute(new CommandWithNullableConcept(null));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
