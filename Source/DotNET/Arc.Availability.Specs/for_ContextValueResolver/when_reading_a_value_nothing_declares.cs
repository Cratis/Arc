// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_reading_a_value_nothing_declares : Specification
{
    ContextValueOutcome _outcome;

    async Task Because() => _outcome = await Requests.For(null).Resolve("unknown");

    [Fact] void should_be_absent() => _outcome.IsPresent.ShouldBeFalse();
}
