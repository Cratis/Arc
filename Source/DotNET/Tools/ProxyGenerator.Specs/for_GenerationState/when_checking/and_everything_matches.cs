// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GenerationState.when_checking;

public class and_everything_matches : given_a_saved_state
{
    void Because() => Result = GenerationState.IsUpToDate(StateFile, Fingerprint);
    [Fact] void should_be_up_to_date() => Result.ShouldBeTrue();
}
