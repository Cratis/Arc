// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GenerationState.when_checking;

public class and_an_output_was_modified : given_a_saved_state
{
    void Establish() => File.AppendAllText(Output, "changed");
    void Because() => Result = GenerationState.IsUpToDate(StateFile, Fingerprint);
    [Fact] void should_invalidate_the_incremental_state() => Result.ShouldBeFalse();
}
