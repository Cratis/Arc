// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GenerationState.when_checking;

public class and_the_state_is_unreadable : given_a_saved_state
{
    void Establish() => File.WriteAllText(StateFile, "not json");
    void Because() => Result = GenerationState.IsUpToDate(StateFile, Fingerprint);
    [Fact] void should_regenerate_without_throwing() => Result.ShouldBeFalse();
}
